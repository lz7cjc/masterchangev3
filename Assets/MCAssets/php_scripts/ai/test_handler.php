<?php
// test_handler.php
require_once 'configuration.php';

$config = new JConfig();

if ($config->debug) {
    header('Content-Type: text/plain');
    error_reporting(E_ALL);
    ini_set('display_errors', 1);
    ini_set('log_errors', 1);
    ini_set('error_log', 'debug_errors.log');

    // Add CORS headers
    header('Access-Control-Allow-Origin: *');
    header('Access-Control-Allow-Methods: POST, OPTIONS');
    header('Access-Control-Allow-Headers: Content-Type');

    // Log debug info
    error_log("=== New Debug Request " . date('Y-m-d H:i:s') . " ===");

    // Handle preflight requests
    if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
        http_response_code(200);
        exit();
    }

    echo "=== Debug Output ===\n\n";

    echo "1. Request Information:\n";
    echo "----------------------\n";
    echo "Time: " . date('Y-m-d H:i:s') . "\n";
    echo "Method: " . $_SERVER['REQUEST_METHOD'] . "\n";
    echo "Content-Type: " . ($_SERVER['CONTENT_TYPE'] ?? 'not set') . "\n";
    echo "Request URI: " . $_SERVER['REQUEST_URI'] . "\n";
    echo "Script Name: " . $_SERVER['SCRIPT_NAME'] . "\n";
    echo "HTTP Host: " . $_SERVER['HTTP_HOST'] . "\n\n";

    echo "2. Raw Input:\n";
    echo "------------\n";
    $raw_input = file_get_contents('php://input');
    echo $raw_input . "\n\n";

    echo "3. Decoded Input:\n";
    echo "---------------\n";
    $input = json_decode($raw_input, true);
    if (json_last_error() === JSON_ERROR_NONE) {
        echo "Successfully decoded JSON:\n";
        echo json_encode($input, JSON_PRETTY_PRINT) . "\n";
    } else {
        echo "JSON decode error: " . json_last_error_msg() . "\n";
    }
    echo "\n";

    echo "4. POST Data:\n";
    echo "------------\n";
    echo json_encode($_POST, JSON_PRETTY_PRINT) . "\n\n";

    echo "5. Server Variables:\n";
    echo "-----------------\n";
    $important_vars = [
        'REQUEST_METHOD',
        'CONTENT_TYPE',
        'CONTENT_LENGTH',
        'HTTP_ORIGIN',
        'HTTP_HOST',
        'HTTP_USER_AGENT',
        'HTTPS',
        'REMOTE_ADDR',
        'SERVER_SOFTWARE',
        'SCRIPT_FILENAME',
        'DOCUMENT_ROOT'
    ];

    foreach ($important_vars as $var) {
        echo "$var: " . ($_SERVER[$var] ?? 'not set') . "\n";
    }
    echo "\n";

    echo "6. PHP Information:\n";
    echo "----------------\n";
    echo "PHP Version: " . phpversion() . "\n";
    echo "Loaded Extensions: " . implode(', ', get_loaded_extensions()) . "\n\n";

    echo "7. Directory Information:\n";
    echo "---------------------\n";
    echo "Current Directory: " . getcwd() . "\n";
    echo "Script Location: " . __FILE__ . "\n\n";

    // Test database connection if dbconnect.php exists
    echo "8. Database Connection Test:\n";
    echo "------------------------\n";
    if (file_exists('dbconnect.php')) {
        echo "dbconnect.php found, testing connection...\n";
        try {
            require_once('dbconnect.php');
            if (isset($conn) && $conn instanceof mysqli) {
                echo "Database connection successful\n";
                echo "Server info: " . $conn->server_info . "\n";
                echo "Host info: " . $conn->host_info . "\n";
            } else {
                echo "Database connection variable not properly initialized\n";
            }
        } catch (Exception $e) {
            echo "Database connection error: " . $e->getMessage() . "\n";
        }
    } else {
        echo "dbconnect.php not found in current directory\n";
    }
    echo "\n";

    // Log completion
    error_log("Debug request completed");
    echo "=== Debug Output Complete ===\n";
} else {
    echo "Debugging is disabled.";
}
?>
