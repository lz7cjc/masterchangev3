<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "masterchange";*/

//remote machine
//remote machine
/*$servername = "10.16.16.15";
$username = "lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "lz7cj-2go-u-259938";
*/

require("dbconnect.php");



//variables from Unity
/*	$userid = $_POST["dbuserid"];
 	$description = $_POST["description"];
 	$rirosBought = $_POST["rirosBought"];
 	$rirosEarnt = $_POST["rirosEarnt"];
 	$rirosSpent = $_POST["rirosSpent"];*/

//variables hardcoded
	$userid = "678";
 	$description = "this is the test variable";
 	$rirosBought = "500";
 	$rirosEarnt = "1800";
 	$rirosSpent = "8282";
echo "userid" . $userid . "description" . $description . "rirosBought" . $rirosBought;
//update riros table

$insertriros = "INSERT INTO riros(userid, rirosEarnt, rirosSpent, rirosBought, description) VALUES ('" . $userid . "','" . $rirosEarnt . $rirosSpent . "','" . $rirosBought . "','" . $description . "');";

echo $insertriros;

mysqli_query($conn, $insertriros) or die("4: insert player query failed" . $insertriros); //insert data or error for failure
$result = $conn->query($user_idquery);

if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}
echo json_encode($dbdata);




$conn->close();

?>