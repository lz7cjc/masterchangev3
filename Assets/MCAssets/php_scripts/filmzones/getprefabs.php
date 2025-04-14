<?php
// Enable error reporting for debugging
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

header('Content-Type: application/json');

// Path to the prefabs.json file exported from Unity
$prefabsFilePath = $_SERVER['DOCUMENT_ROOT'] . "/php_scripts/filmzones/WebExport/prefabs.json";

try {
    if (file_exists($prefabsFilePath)) {
        // Read the JSON file
        $prefabsJson = file_get_contents($prefabsFilePath);
        echo $prefabsJson;
    } else {
        // Fallback to default prefabs if file not found
        $fallbackPrefabs = [
            "prefabs" => [
                "Default",
                "BeachSignpost1",
                "Carnival",
                "Football-brazil", 
                "OvalSignPost"
            ]
        ];
        echo json_encode($fallbackPrefabs);
        
        // Log error for debugging
        error_log("Prefabs file not found at: $prefabsFilePath");
    }
} catch (Exception $e) {
    // Return error
    http_response_code(500);
    echo json_encode([
        'success' => false, 
        'error' => $e->getMessage(),
        'debug' => [
            'php_version' => PHP_VERSION,
            'server_info' => $_SERVER['SERVER_SOFTWARE'] ?? 'unknown',
            'document_root' => $_SERVER['DOCUMENT_ROOT'],
            'prefabs_path' => $prefabsFilePath,
            'last_error' => error_get_last()
        ]
    ]);
}
?>