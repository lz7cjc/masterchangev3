<?php
require('dbconnect.php');

header('Content-Type: application/json');
error_reporting(E_ALL);
ini_set('display_errors', 1);
ini_set('log_errors', 1);

// Clear the log file at the beginning of each run
file_put_contents('ai_content_errors.log', '');

ini_set('error_log', 'ai_content_errors.log');

$input = json_decode(file_get_contents('php://input'), true);

if (json_last_error() !== JSON_ERROR_NONE) {
    echo json_encode(['error' => 'Invalid JSON input']);
    exit();
}

$userId = $input['userId'] ?? null;
$contentLength = $input['contentLength'] ?? 100;
$numPrompts = $input['numPrompts'] ?? 1;
$numDetails = $input['numDetails'] ?? 3; // Default to 3 if not provided

if (!$userId) {
    echo json_encode(['error' => 'Missing required parameters']);
    exit();
}

// Debug logging
error_log("Input Parameters: userId = $userId, contentLength = $contentLength, numPrompts = $numPrompts, numDetails = $numDetails");

$sql = "SELECT DISTINCT
    bt.Behaviour_Type AS behaviourname,
    h.label AS habit_name,
    h.description AS habit_description,
    uh.amount,
    uh.yesorno,
    bt.BehaviourType_ID AS behaviourtypeid
FROM
    userhabits uh
    INNER JOIN users u ON u.User_ID = uh.user_id
    INNER JOIN user_behaviour ub ON ub.userid = u.User_ID
    INNER JOIN behaviour_type bt ON bt.BehaviourType_ID = ub.behaviourtypeid
    INNER JOIN habits h ON h.Habit_ID = uh.habit_id
WHERE
    u.User_ID = ? AND ub.interested = true AND bt.BehaviourType_ID = h.habitsectionid AND (uh.yesorno = true OR uh.amount IS NOT NULL)
ORDER BY RAND()
LIMIT ?";

$stmt = $conn->prepare($sql);

if ($stmt === false) {
    error_log('SQL prepare failed: ' . $conn->error);
    echo json_encode(['error' => 'SQL prepare failed: ' . $conn->error]);
    exit();
}

$stmt->bind_param("ii", $userId, $numPrompts);
$stmt->execute();
$result = $stmt->get_result();

// Debug logging
error_log("SQL Query executed. Number of rows returned: " . $result->num_rows);

$prompts = [];

while ($row = $result->fetch_assoc()) {
    $prompt = "Generate a tip of between $contentLength plus or minus 50 characters about " . $row['behaviourname'] . " for someone who:";

    // Subquery to get random detailed bullet points
    $detailsSql = "SELECT h.label AS habit_name, h.description AS habit_description, uh.amount, uh.yesorno
                   FROM userhabits uh
                   INNER JOIN habits h ON h.Habit_ID = uh.habit_id
                   WHERE uh.user_id = ? AND h.habitsectionid = ?
                   ORDER BY RAND()
                   LIMIT ?";

    $detailsStmt = $conn->prepare($detailsSql);
    $detailsStmt->bind_param("iii", $userId, $row['behaviourtypeid'], $numDetails);
    $detailsStmt->execute();
    $detailsResult = $detailsStmt->get_result();

    $hasDetails = false;
    while ($detailsRow = $detailsResult->fetch_assoc()) {
        if ($detailsRow['amount']) {
            $prompt .= "\n- " . $detailsRow['habit_name'] . ": " . $detailsRow['amount'] . " (" . $detailsRow['habit_description'] . ")";
            $hasDetails = true;
        }
        if ($detailsRow['yesorno']) {
            $prompt .= "\n- " . $detailsRow['habit_name'] . ": " . $detailsRow['habit_description'];
            $hasDetails = true;
        }
    }

    if (!$hasDetails) {
        $prompt .= "\n- No specific details available.";
    }

    $prompts[] = $prompt;
}

// Debug logging
error_log("Generated Prompts: " . print_r($prompts, true));

echo json_encode(['prompts' => $prompts]);

$conn->close();
?>
