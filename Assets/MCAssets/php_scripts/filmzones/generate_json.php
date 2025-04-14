<?php
// Enable error reporting for debugging
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

header('Content-Type: application/json');

// Database config
$servername = "localhost";
$username = "vdl54bm_lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "vdl54bm_lz7cj-2g";

// Connect
$conn = new mysqli($servername, $username, $password, $dbname);
if ($conn->connect_error) {
    http_response_code(500);
    echo json_encode(["success" => false, "error" => "Database connection failed: " . $conn->connect_error]);
    exit;
}

try {
    // Get films with zones - using a simpler JOIN structure and including prefab field
    $sql = "SELECT f.filename AS filmname, f.public_url AS publicURL, f.Label, f.Description, 
            f.Country, f.Locale, f.Prefab, GROUP_CONCAT(z.Zone_name SEPARATOR ', ') AS zones
            FROM filmsfrombucket f 
            LEFT JOIN filmzones fz ON f.filmid = fz.filmsfrombucketid 
            LEFT JOIN zones z ON fz.zonesid = z.Zone_id
            GROUP BY f.filmid";

    $result = $conn->query($sql);

    if (!$result) {
        throw new Exception("Query failed: " . $conn->error);
    }

    $films = [];
    while ($row = $result->fetch_assoc()) {
        $films[] = $row;
    }

    $jsonData = json_encode($films, JSON_PRETTY_PRINT);
    if ($jsonData === false) {
        throw new Exception("JSON encoding failed: " . json_last_error_msg());
    }

    // Ensure the directory exists
    $directory = $_SERVER['DOCUMENT_ROOT'] . "/films";
    if (!is_dir($directory)) {
        if (!mkdir($directory, 0755, true)) {
            throw new Exception("Failed to create directory: $directory");
        }
    }

    // Check if the directory is writable
    if (!is_writable($directory)) {
        throw new Exception("Directory is not writable: $directory");
    }

    // Full path to the output file
    $outputPath = $directory . "/film_data.json";

    // Write the file
    $bytesWritten = file_put_contents($outputPath, $jsonData);
    if ($bytesWritten === false) {
        throw new Exception("Failed to write file: " . error_get_last()['message']);
    }

    echo json_encode([
        'success' => true, 
        'path' => '/films/film_data.json',
        'bytes_written' => $bytesWritten,
        'film_count' => count($films)
    ]);

} catch (Exception $e) {
    http_response_code(500);
    echo json_encode([
        'success' => false, 
        'error' => $e->getMessage(),
        'debug' => [
            'php_version' => PHP_VERSION,
            'server_info' => $_SERVER['SERVER_SOFTWARE'] ?? 'unknown',
            'document_root' => $_SERVER['DOCUMENT_ROOT'],
            'last_error' => error_get_last()
        ]
    ]);
}

$conn->close();
?>