<?php
require("dbconnect.php");

//Values from Unity
/*$fieldName = $_POST["fieldName"];
 $fieldValue = $_POST["fieldValue"];
 $userid = $_POST["user"];*/

//hardcoded values
/*$age = "34";
 $weight = "98";
 $height = "180";
$userid = "450";*/
  ///////////////////////////////
	//DB values for the habits
$fieldName = "JCP";
 $fieldValue = "33";
 $userid = "373";



/////////// ************** INSERT AND UPDATE VALUE
	echo "<br> ---------------------------------<br>Start of insert/update for #user_preferences <br> ---------------------------------<br>";
$Checkquery = "SELECT * FROM `user_preferences` where dbuserid  = '" . $userid . "';"; 
//echo "ageCheckquery: " . $ageCheckquery . "<br>";
$checkPP = mysqli_query($conn, $Checkquery) or die("2:age check query failed");

if(mysqli_num_rows($checkPP) >0)
{
	echo "3: habit already exists";
	$updateprefvalue = "UPDATE user_preferences SET " . $fieldName  . "=" . $fieldValue . " WHERE dbuserid  = " . $userid . ";"; 
echo "updateprefvalue:" . $updateprefvalue . "<br>";

mysqli_query($conn, $updateprefvalue) or die("4: update to prefs failed "); //insert data or error for failure
	
  
  
}
else
{
	echo "3: new record";
	$insertprefs = "INSERT INTO user_preferences (dbuserid, " . $fieldName . ") VALUES ('" . $userid . "','" . $fieldValue .  "');";
	echo "insertprefs:" . $insertprefs . "<br>";


	mysqli_query($conn, $insertprefs) or die("4: insert prefs failed"); //insert data or error for failure
}
	echo "<br> ---------------------------------<br>End of insert/update for #age <br> ---------------------------------<br>";





$conn->close();

?>