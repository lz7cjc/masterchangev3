<?php
require("dbconnect.php");
//from unity
$rirosValue = $_POST["rirosValue"];
$description = $_POST["description"];
 $riroType = $_POST["riroType"];
$userid = $_POST["userid"];


//Hard coded values
/* $rirosValue= "650";
 $riroType= "Spent";
 $userid = "2587";
$description = "tip for fim";*/

if ($riroType == "Bought")
{
$updateRiros = "INSERT INTO riros(userid, rirosBought, description) VALUES ('" . $userid . "', '" . $rirosValue . "', '" . $description . "');";

}

else if ($riroType == "Earnt")
{
$updateRiros = "INSERT INTO riros(userid, rirosEarnt, description) VALUES ('" . $userid . "', '" . $rirosValue . "', '" . $description . "');";

}

else if ($riroType == "Spent")
{
	$updateRiros = "INSERT INTO riros(userid, rirosSpent, description) VALUES ('" . $userid . "', '" . $rirosValue . "', '" . $description . "');";

}
mysqli_query($conn, $updateRiros) or die("4: update to riros failed"); //insert data or error for failure
	
 
$conn->close();

?>