<?php
require('dbconnect.php');

if ($conn->connect_error) {
    die("Connection failed: " . $conn->connect_error);
} else {
    echo "Connected successfully\n";
}

$tables = ['userhabits', 'users', 'user_behaviour', 'behaviour_type', 'habits'];

foreach ($tables as $tableName) {
    echo "Columns in the table '$tableName':\n";
    $sql = "SHOW COLUMNS FROM $tableName";
    $result = $conn->query($sql);

    if ($result->num_rows > 0) {
        while ($row = $result->fetch_assoc()) {
            echo $row['Field'] . "\n";
        }
    } else {
        echo "No columns found or table '$tableName' does not exist.\n";
    }
    echo "\n";
}

$conn->close();
?>

