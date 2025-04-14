<?php
include 'db.php';

$sql = "SELECT * FROM filmsfrombucket";
$result = $conn->query($sql);

$films = [];
while ($row = $result->fetch_assoc()) {
    $films[] = $row;
}

echo json_encode($films);
?>
