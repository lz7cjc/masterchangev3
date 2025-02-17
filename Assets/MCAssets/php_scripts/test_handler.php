<?php
header('Content-Type: text/plain');
error_reporting(E_ALL);
ini_set('display_errors', 1);
ini_set('error_log', 'ai_content_errors.log'); // Set custom error log file

// Add CORS headers
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit();
}

$input = json_decode(file_get_contents('php://input'), true);

if (json_last_error() !== JSON_ERROR_NONE) {
    error_log("Error parsing JSON request: " . json_last_error_msg());
    die("Error parsing JSON request: " . json_last_error_msg());
}

// Extract and validate configuration
$userId = $input['userId'] ?? null;
$contentTypes = $input['contentTypes'] ?? null;
$aiEndpoint = $input['aiEndpoint'] ?? "/php_scripts/ai/aigeneratecontent_debug.php";
$fallbackEndpoint = $input['fallbackEndpoint'] ?? "/php_scripts/allmytips.php";
$maxTokens = $input['maxTokens'] ?? null;
$temperature = $input['temperature'] ?? 0.7;

if (!$userId || !$contentTypes) {
    error_log("Error: Missing required parameters (userId or contentTypes)");
    die("Error: Missing required parameters (userId or contentTypes)");
}

class ContentGenerationTest
{
    private $userId;
    private $contentTypes;
    private $config;
    private $startTime;

    public function __construct($userId, $contentTypes, $config)
    {
        $this->userId = $userId;
        $this->contentTypes = $contentTypes;
        $this->config = $config;
        $this->startTime = microtime(true);
    }

    public function runTest()
    {
        echo "Starting Content Generation Test\n";
        echo "--------------------------------\n";
        echo "Test Configuration:\n";
        echo "- User ID: " . $this->userId . "\n";
        echo "- Number of Content Types: " . count($this->contentTypes) . "\n";
        echo "- Content Types: " . implode(", ", $this->contentTypes) . "\n";
        echo "- Temperature: " . $this->config['temperature'] . "\n";
        if ($this->config['maxTokens']) {
            echo "- Max Tokens Override: " . $this->config['maxTokens'] . "\n";
        }
        echo "\n";

        try {
            $result = $this->simulateUnityRequest();
            echo "Primary API Response:\n";
            echo "- HTTP Status Code: " . $result['status_code'] . "\n";
            echo "- Response Time: " . number_format((microtime(true) - $this->startTime) * 1000, 2) . "ms\n\n";

            if ($result['response'] === "0 results") {
                echo "No content generated, attempting fallback...\n\n";
                $fallbackResult = $this->simulateFallbackRequest();

                if ($fallbackResult['response'] === "0 results") {
                    echo "Fallback also returned no results\n";
                    return false;
                }
                $result = $fallbackResult;
            }

            $decodedResponse = json_decode($result['response'], true);

            if (json_last_error() !== JSON_ERROR_NONE) {
                echo "Error: Failed to decode JSON response\n";
                echo "JSON Error: " . json_last_error_msg() . "\n";
                echo "Raw Response: " . $result['response'] . "\n";
                return false;
            }

            echo "Test Result: Success\n";
            echo "Number of content items received: " . (isset($decodedResponse['data']) ? count($decodedResponse['data']) : 0) . "\n\n";

            echo "Generated Content:\n";
            echo "----------------\n";
            if (isset($decodedResponse['data']) && is_array($decodedResponse['data'])) {
                foreach ($decodedResponse['data'] as $index => $content) {
                    echo "Content " . ($index + 1) . " (Type " . $this->contentTypes[$index] . "):\n";
                    echo "Length: " . strlen($content['ContentBody']) . " characters\n";
                    echo "Content:\n" . $content['ContentBody'] . "\n\n";
                }
            }

            if (isset($decodedResponse['data']) && count($decodedResponse['data']) !== count($this->contentTypes)) {
                echo "Warning: Number of responses (" . count($decodedResponse['data']) .
                    ") doesn't match number of requested content types (" . count($this->contentTypes) . ")\n";
            }

            return true;
        } catch (Exception $e) {
            echo "Error: Test execution failed\n";
            echo "Error Message: " . $e->getMessage() . "\n";
            error_log("Error: Test execution failed - " . $e->getMessage());
            return false;
        }
    }

    private function simulateUnityRequest()
    {
        $postData = array(
            'userid' => $this->userId,
            'contenttype' => $this->contentTypes[0], // For single type requests
            'contenttypes' => json_encode($this->contentTypes) // For multi-type requests
        );

        if ($this->config['maxTokens']) {
            $postData['max_tokens'] = $this->config['maxTokens'];
        }
        if ($this->config['temperature']) {
            $postData['temperature'] = $this->config['temperature'];
        }

        return $this->makeRequest($this->config['aiEndpoint'], $postData);
    }

    private function simulateFallbackRequest()
    {
        $postData = array(
            'userid' => $this->userId,
            'contenttype' => $this->contentTypes[0], // For single type requests
            'contenttypes' => json_encode($this->contentTypes), // For multi-type requests
            'title' => 0
        );

        return $this->makeRequest($this->config['fallbackEndpoint'], $postData);
    }

    private function makeRequest($endpoint, $postData)
    {
        // Ensure endpoint starts with http:// or https:// or /
        if (!preg_match('~^(https?://|/)~i', $endpoint)) {
            $endpoint = '/' . $endpoint;
        }

        // If it's a relative path, convert to full URL
        if (strpos($endpoint, 'http') !== 0) {
            $protocol = isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on' ? 'https://' : 'http://';
            $endpoint = $protocol . $_SERVER['HTTP_HOST'] . $endpoint;
        }

        echo "Making request to: " . $endpoint . "\n";

        $ch = curl_init($endpoint);
        if ($ch === false) {
            throw new Exception("Failed to initialize CURL");
        }

        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_POST, true);
        curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($postData));
        curl_setopt($ch, CURLOPT_CONNECTTIMEOUT, 10);
        curl_setopt($ch, CURLOPT_TIMEOUT, 30);
        curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false); // For development
        curl_setopt($ch, CURLOPT_VERBOSE, true);
        curl_setopt($ch, CURLOPT_HTTPHEADER, array(
            'Content-Type: application/json'
        ));

        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);

        if (curl_errno($ch)) {
            echo "Curl Error: " . curl_error($ch) . "\n";
            echo "Endpoint attempted: " . $endpoint . "\n";
            error_log("Curl Error: " . curl_error($ch) . " - Endpoint: " . $endpoint);
        }

        curl_close($ch);

        return [
            'status_code' => $httpCode,
            'response' => $response
        ];
    }
}

// Create and run the test
$test = new ContentGenerationTest($userId, $contentTypes, [
    'aiEndpoint' => $aiEndpoint,
    'fallbackEndpoint' => $fallbackEndpoint,
    'maxTokens' => $maxTokens,
    'temperature' => $temperature
]);

$test->runTest();
?>
