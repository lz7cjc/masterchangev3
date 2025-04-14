<?php
$servername = "localhost";
$username = "vdl54bm_lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "vdl54bm_lz7cj-2g";

$conn = new mysqli($servername, $username, $password, $dbname);

if ($conn->connect_error) {
    die("❌ Connection failed: " . $conn->connect_error);
}

$sql = "SELECT * FROM filmsfrombucket LIMIT 5";
$result = $conn->query($sql);

if ($result->num_rows > 0) {
    echo "✅ Connected and data exists:<br><br>";
    while($row = $result->fetch_assoc()) {
        echo "🎬 " . $row["filename"] . " — " . $row["Label"] . "<br>";
    }
} else {
    echo "⚠️ Connected, but no data found in filmsfrombucket.";
}

$conn->close();
?>
