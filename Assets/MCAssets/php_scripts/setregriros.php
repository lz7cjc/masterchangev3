<?php
require("dbconnect.php");

//from unity
$rirosEarnt = $_POST["rirosEarnt"];
$rirosSpent = $_POST["rirosSpent"];
$description = $_POST["description"];
$userid = $_POST["userid"];


//Hard coded values
// $rirosEarnt = 18500;
// $rirosSpent = 650;
// $description = "Registration";
// $userid = 666556000;

$updateRiros = "INSERT INTO riros(userid, rirosEarnt, rirosSpent, description) VALUES ('" . $userid . "', '" . $rirosEarnt . "', '" . $rirosSpent . "', '" . $description . "');";

echo $updateRiros;
mysqli_query($conn, $updateRiros) or die("4: update to riros failed"); //insert data or error for failure
	


$conn->close();

?>