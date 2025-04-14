<?php
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
    echo json_encode(["error" => "Database connection failed"]);
    exit;
}

// Query films with their assigned zones and include the Prefab field
$sql = "
    SELECT 
        f.filmid, 
        f.filename, 
        f.public_url, 
        f.Label, 
        f.Description, 
        f.Country, 
        f.Locale,
        f.Prefab,
        GROUP_CONCAT(z.Zone_name SEPARATOR ', ') AS Zones
    FROM filmsfrombucket f
    LEFT JOIN filmzones fz ON f.filmid = fz.filmsfrombucketid
    LEFT JOIN zones z ON fz.zonesid = z.Zone_id
    GROUP BY f.filmid
";

$result = $conn->query($sql);

if (!$result) {
    http_response_code(500);
    echo json_encode(["error" => "Query failed: " . $conn->error]);
    exit;
}

$films = [];
while ($row = $result->fetch_assoc()) {
    $films[] = $row;
}

echo json_encode($films);
$conn->close();