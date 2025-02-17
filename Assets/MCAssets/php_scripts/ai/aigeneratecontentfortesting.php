<?php

// Debugging: Log the current working directory
error_log("Current working directory: " . getcwd());
ini_set('error_log', 'ai_content_errors.log'); // Set custom error log file

$dbconnect_path = __DIR__ . "/dbconnect.php";
if (!file_exists($dbconnect_path)) {
    error_log("Error: dbconnect.php not found at " . $dbconnect_path);
    http_response_code(500);
    echo json_encode(['error' => 'Internal Server Error']);
    exit();
}

require($dbconnect_path);
require(__DIR__ . '/configuration.php'); // Include the configuration file

$config = new JConfig(); // Create an instance of the configuration class
$api_key = $config->apiKey; // Get the API key from the configuration

error_reporting(E_ALL);
ini_set('display_errors', 1);
ini_set('log_errors', 1);
ini_set('error_log', 'ai_content_errors.log');

// Add CORS headers for development
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

// Handle preflight requests
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit();
}

// Ensure we're working with JSON data
$input = json_decode(file_get_contents('php://input'), true);

// Log the input for debugging
error_log("Received input: " . print_r($input, true));

// Validate input parameters
$userid = isset($input['userid']) ? intval($input['userid']) : null;
$contentLength = isset($input['contentLength']) ? intval($input['contentLength']) : 100;
$maxTokens = isset($input['maxTokens']) ? intval($input['maxTokens']) : null;
$temperature = isset($input['temperature']) ? floatval($input['temperature']) : 0.7;
$numDetails = isset($input['numDetails']) ? intval($input['numDetails']) : 3; // Default to 3 if not provided
$numPrompts = isset($input['numPrompts']) ? intval($input['numPrompts']) : 5; // Default to 5 if not provided

if (!$userid) {
    error_log("Error: Missing required parameters");
    http_response_code(400);
    echo json_encode(['error' => 'Missing required parameters']);
    exit();
}

// Get user's behavior data
$sql = "SELECT DISTINCT
    bt.Behaviour_Type AS behaviourname,
    h.label AS habit_name,
    uh.amount,
    uh.yesorno,
    YEAR(CURDATE()) - YEAR(uh.datetime) AS duration
FROM
    userhabits uh
    INNER JOIN users u ON u.User_ID = uh.user_id
    INNER JOIN user_behaviour ub ON ub.userid = u.User_ID
    INNER JOIN behaviour_type bt ON bt.BehaviourType_ID = ub.behaviourtypeid
    INNER JOIN habits h ON h.Habit_ID = uh.habit_id
WHERE
    u.User_ID = ?
ORDER BY RAND()";

error_log("Executing SQL query: " . $sql);

try {
    $stmt = $conn->prepare($sql);
    if (!$stmt) {
        throw new Exception("Prepare failed: " . $conn->error);
    }

    $stmt->bind_param("i", $userid);
    if (!$stmt->execute()) {
        throw new Exception("Execute failed: " . $stmt->error);
    }

    $result = $stmt->get_result();

    $rows = [];
    while ($row = $result->fetch_assoc()) {
        $rows[] = $row;
    }

    if (count($rows) > 0) {
        // Filter rows to prioritize those with the requisite number of detailed descriptions
        $filteredRows = array_filter($rows, function ($row) {
            return !(
                strpos($row['habit_name'], "No specific details available") !== false ||
                strpos($row['amount'], "No specific details available") !== false ||
                strpos($row['duration'], "No specific details available") !== false ||
                strpos($row['yesorno'], "No specific details available") !== false
            );
        });

        if (count($filteredRows) > 0) {
            $row = $filteredRows[array_rand($filteredRows)];
        } else {
            $row = $rows[array_rand($rows)];
        }

        error_log("Database query successful. Found behavior data: " . print_r($row, true));

        // Build AI prompt
        $prompt = "Generate a helpful tip of between $contentLength plus or minus 50 characters about " . $row['behaviourname'] . " for someone who has the following details:";
        $details = [
            "Habit Name: " . $row['habit_name'],
            "Amount: " . ($row['amount'] ?? 'N/A'),
            "Duration: " . ($row['duration'] ?? 'N/A') . " years",
            "Suffers from: " . ($row['yesorno'] ? "Yes" : "No")
        ];

        // Filter out "No specific details available" and limit to numDetails
        $details = array_filter($details, function ($detail) {
            return strpos($detail, "No specific details available") === false;
        });
        $details = array_slice($details, 0, $numDetails);

        foreach ($details as $detail) {
            $prompt .= "\n- " . $detail;
        }

        error_log("Generated prompt: " . $prompt);

        $ch = curl_init('https://api.openai.com/v1/chat/completions');
        $data = array(
            'model' => 'gpt-3.5-turbo',
            'messages' => array(
                array(
                    'role' => 'system',
                    'content' => 'You are a personalized content generator.'
                ),
                array(
                    'role' => 'user',
                    'content' => $prompt
                )
            ),
            'max_tokens' => $maxTokens ?? getMaxTokens($contentLength),
            'temperature' => $temperature ?? 0.7
        );

        error_log("OpenAI API request data: " . json_encode($data));

        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_POST, true);
        curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
        curl_setopt($ch, CURLOPT_HTTPHEADER, array(
            'Content-Type: application/json',
            'Authorization: Bearer ' . $api_key // Use the API key from the configuration
        ));

        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);

        error_log("OpenAI API Response Code: " . $httpCode);
        error_log("OpenAI API Response: " . $response);

        if (curl_errno($ch)) {
            error_log("Curl error: " . curl_error($ch));
        }

        curl_close($ch);

        if ($response) {
            $aiResponse = json_decode($response, true);
            error_log("Full OpenAI API Response: " . print_r($aiResponse, true));
            if (isset($aiResponse['choices'][0]['message']['content'])) {
                $content = $aiResponse['choices'][0]['message']['content'];
                $dbdata = array(array('ContentBody' => $content));

                // Clear any buffered output before sending response
                ob_clean();
                echo json_encode(array('data' => $dbdata, 'prompt' => $prompt));
                error_log("Successfully generated and returned content");
                exit;
            } else {
                error_log("Error: Unexpected API response structure");
            }
        }
    } else {
        error_log("No behavior data found for user ID: " . $userid);
    }
} catch (Exception $e) {
    error_log("Error in script execution: " . $e->getMessage());
    error_log("Stack trace: " . $e->getTraceAsString());
}

// Clear any buffered output before sending fallback response
ob_clean();
error_log("Returning '0 results' to trigger fallback");
echo "0 results";

function getMaxTokens($contentLength)
{
    // Estimate max tokens based on content length
    // Roughly 4 characters per token
    return ceil($contentLength / 4);
}

$conn->close();
?>
