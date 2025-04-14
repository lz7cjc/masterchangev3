<?php
// Enable error reporting for debugging
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

// Database config
$servername = "localhost";
$username = "vdl54bm_lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "vdl54bm_lz7cj-2g";

// Send JSON response
header('Content-Type: application/json');

// Log request data
$request_log = [
    'post_data' => $_POST,
    'raw_input' => file_get_contents('php://input')
];

// Connect to database
$conn = new mysqli($servername, $username, $password, $dbname);
if ($conn->connect_error) {
    echo json_encode([
        "success" => false, 
        "error" => "Database connection failed: " . $conn->connect_error,
        "debug" => $request_log
    ]);
    exit;
}

// Get values from form
$filmid = isset($_POST['filmid']) ? intval($_POST['filmid']) : 0;
$Label = isset($_POST['Label']) ? $_POST['Label'] : null;
$Description = isset($_POST['Description']) ? $_POST['Description'] : null;
$Country = isset($_POST['Country']) ? $_POST['Country'] : null;
$Locale = isset($_POST['Locale']) ? $_POST['Locale'] : null;
$Prefab = isset($_POST['Prefab']) ? $_POST['Prefab'] : null;
$zones = isset($_POST['zones']) && is_array($_POST['zones']) ? $_POST['zones'] : [];

// Make sure we have a valid film ID
if ($filmid <= 0) {
    echo json_encode([
        "success" => false, 
        "error" => "Invalid film ID", 
        "debug" => array_merge($request_log, ["filmid" => $filmid])
    ]);
    exit;
}

$log = [
    "received_data" => [
        "filmid" => $filmid,
        "Label" => $Label,
        "Description" => $Description,
        "Country" => $Country, 
        "Locale" => $Locale,
        "Prefab" => $Prefab,
        "zones" => $zones
    ],
    "request" => $request_log
];

try {
    // Check if film exists before updating
    $check = $conn->prepare("SELECT filmid FROM filmsfrombucket WHERE filmid = ?");
    $check->bind_param("i", $filmid);
    $check->execute();
    $result = $check->get_result();
    
    if ($result->num_rows === 0) {
        throw new Exception("Film with ID $filmid not found");
    }
    
    $log["film_exists"] = true;
    
    // Update film info - handle NULL values properly and add Prefab field
    $stmt = $conn->prepare("UPDATE filmsfrombucket SET Label=?, Description=?, Country=?, Locale=?, Prefab=? WHERE filmid=?");
    if (!$stmt) {
        throw new Exception("Prepare failed: " . $conn->error);
    }
    
    $stmt->bind_param("sssssi", $Label, $Description, $Country, $Locale, $Prefab, $filmid);
    
    if (!$stmt->execute()) {
        throw new Exception("Execute failed: " . $stmt->error);
    }
    
    $log["film_update"] = "Success";
    $log["affected_rows"] = $stmt->affected_rows;
    
    // Update zones - first delete existing
    $delete_result = $conn->query("DELETE FROM filmzones WHERE filmsfrombucketid = $filmid");
    if (!$delete_result) {
        throw new Exception("Delete zones failed: " . $conn->error);
    }
    
    $log["zones_delete"] = "Success";
    $log["zones_delete_affected"] = $conn->affected_rows;
    
    // Insert new zones if any
    if (!empty($zones)) {
        $log["zones_to_insert"] = $zones;
        $successful_inserts = 0;
        
        foreach ($zones as $zoneid) {
            $zoneid = intval($zoneid);
            
            // Simple query instead of prepare to avoid potential issues
            $insert_result = $conn->query("INSERT INTO filmzones (filmsfrombucketid, zonesid) VALUES ($filmid, $zoneid)");
            
            if (!$insert_result) {
                $log["zone_insert_error_" . $zoneid] = $conn->error;
            } else {
                $successful_inserts++;
            }
        }
        
        $log["zones_insert_success_count"] = $successful_inserts;
    } else {
        $log["zones_insert"] = "No zones to insert";
    }
    
    echo json_encode([
        "success" => true, 
        "message" => "Film updated successfully", 
        "debug" => $log
    ]);
    
} catch (Exception $e) {
    echo json_encode([
        "success" => false, 
        "error" => $e->getMessage(),
        "debug" => $log
    ]);
}

$conn->close();
?>