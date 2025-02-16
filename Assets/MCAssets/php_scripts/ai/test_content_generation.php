<?php
// test_content_generation.php
header('Content-Type: text/plain');

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $input = json_decode(file_get_contents('php://input'), true);
    
    if (json_last_error() !== JSON_ERROR_NONE) {
        die("Error parsing JSON request");
    }

    $userId = $input['userId'] ?? null;
    $contentTypes = $input['contentTypes'] ?? null;

    if (!$userId || !$contentTypes) {
        die("Missing required parameters");
    }

    class ContentGenerationTest {
        private $userId;
        private $contentTypes;
        private $apiEndpoint;

        public function __construct($userId, $contentTypes) {
            $this->userId = $userId;
            $this->contentTypes = $contentTypes;
            $this->apiEndpoint = "https://masterchange.today/php_scripts/aigeneratecontent.php";
        }

        public function runTest() {
            echo "Starting Content Generation Test\n";
            echo "--------------------------------\n";
            echo "User ID: " . $this->userId . "\n";
            echo "Number of Content Types: " . count($this->contentTypes) . "\n";
            echo "Content Types: " . implode(", ", $this->contentTypes) . "\n\n";

            try {
                $result = $this->simulateUnityRequest();
                
                echo "HTTP Status Code: " . $result['status_code'] . "\n\n";
                
                if ($result['response'] === "0 results") {
                    echo "Test Result: No content generated (0 results)\n";
                    return false;
                }

                $decodedResponse = json_decode($result['response'], true);
                
                if (json_last_error() !== JSON_ERROR_NONE) {
                    echo "Test Result: Failed to decode JSON response\n";
                    echo "Error: " . json_last_error_msg() . "\n";
                    echo "Raw Response: " . $result['response'] . "\n";
                    return false;
                }

                echo "Test Result: Success\n";
                echo "Number of content items received: " . count($decodedResponse['data']) . "\n\n";
                
                echo "Generated Content:\n";
                echo "----------------\n";
                foreach ($decodedResponse['data'] as $index => $content) {
                    echo "Content " . ($index + 1) . " (Type " . $this->contentTypes[$index] . "):\n";
                    echo $content['ContentBody'] . "\n\n";
                }

                if (count($decodedResponse['data']) !== count($this->contentTypes)) {
                    echo "Warning: Number of responses (" . count($decodedResponse['data']) . 
                         ") doesn't match number of requested content types (" . count($this->contentTypes) . ")\n";
                }

                return true;
            } catch (Exception $e) {
                echo "Test Result: Error\n";
                echo "Error Message: " . $e->getMessage() . "\n";
                return false;
            }
        }

        private function simulateUnityRequest() {
            $postData = array(
                'userid' => $this->userId,
                'contenttypes' => json_encode($this->contentTypes)
            );

            $ch = curl_init($this->apiEndpoint);
            curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
            curl_setopt($ch, CURLOPT_POST, true);
            curl_setopt($ch, CURLOPT_POSTFIELDS, $postData);

            $response = curl_exec($ch);
            $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
            curl_close($ch);

            return [
                'status_code' => $httpCode,
                'response' => $response
            ];
        }
    }

    $test = new ContentGenerationTest($userId, $contentTypes);
    $test->runTest();
} else {
    echo "Please use the test interface to run tests.";
}
?>