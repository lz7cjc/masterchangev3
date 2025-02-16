<?php


//DB connection - change in included file
require("dbconnect.php");


//Values from Unity
$coworkers = $_POST["formcoworkers"];
 $parents = $_POST["formparents"];
 $friends = $_POST["formfriends"];
$partner = $_POST["formpartner"];
 $alone = $_POST["formalone"];
 $withnonsmokers = $_POST["formwithnonsmokers"];
$withsmokers = $_POST["formwithsmokers"];
 $userid = $_POST["user"];

//hardcoded values
/*$age = "34";
 $weight = "98";
 $height = "180";
$userid = "450";*/
  ///////////////////////////////
	//DB values for the habits
   $IDcoworkers = "35";
   $IDparents = "34";
   $IDfriends = "33";
   $IDpartner = "32";
   $IDalone = "29";
   $InDwithnonsmokers = "30";
   $IDwithsmokers = "31"

/////////// ************** INSERT AND UPDATE CoWorkers
	echo "<br> ---------------------------------<br>Start of insert/update for #age <br> ---------------------------------<br>";
$coworkersCheckquery = "SELECT * from userhabits where habit_id ='" . $IDcoworkers . "' and user_id = '" . $userid . "';"; 
echo "coworkersCheckquery: " . $coworkersCheckquery . "<br>";
$coworkerscheck = mysqli_query($conn, $coworkersCheckquery) or die("2:coworkersCheckquery failed");

if(mysqli_num_rows($coworkerscheck) >0)
{
	echo "3: coworkerscheck habit already exists";
	$updatecoworkers = "UPDATE userhabits SET yesorno = '" . $coworkers . "' WHERE user_id = '" . $userid . "' AND habit_id = '" . $IDcoworkers . "';"; 
echo "updatecoworkers:" . $updatecoworkers . "<br>";

mysqli_query($conn, $updatecoworkers) or die("4: update to age failed "); //insert data or error for failure
	
  
  
}
else
{
	echo "3: new record";
	$insertuserage = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $ageID . "','" . $age .  "');";
	echo "insertuserquerycignum:" . $insertuserage . "<br>";


	mysqli_query($conn, $insertuserage) or die("4: insert age failed"); //insert data or error for failure
}
	echo "<br> ---------------------------------<br>End of insert/update for #age <br> ---------------------------------<br>";


/////////// ************** INSERT AND UPDATE NUMBER OF height
echo "<br> ---------------------------------<br>Start of insert/update for #height <br> ---------------------------------<br>";

$heightCheckquery = "SELECT * from userhabits where habit_id ='" . $heightID . "' and user_id = '" . $userid . "';"; 

$heightcheck = mysqli_query($conn, $heightCheckquery) or die("2:height check query failed");


if(mysqli_num_rows($heightcheck) >0)
{
	  echo "3: height already exists";
	$updateHeight = "UPDATE userhabits SET amount = '" . $height . "' WHERE user_id = '" . $userid . "' AND habit_id = '" . $heightID . "';"; 

	echo "heightcheck:" . $updateHeight . "<br>";

mysqli_query($conn, $updateHeight) or die("4: update to update height failed"); //insert data or error for failure
  

}
else
{
	  echo "New height record";
	$insertHeight = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $heightID . "','" . $height . "');";
		echo "insertHeight:" . $insertHeight . "<br>";

	mysqli_query($conn, $insertHeight) or die("4: insert insert height failed"); //insert data or error for failure
}	
echo "<br> ---------------------------------<br>End of insert/update for #height attempts <br> ---------------------------------<br>";

/////////////////////////////////////////////////////////////////////////////////////////////////
/////////// ************** INSERT AND UPDATE weight

echo "<br> ---------------------------------<br>Start of insert/update for #weight <br> ---------------------------------<br>";
$weightCheckquery = "SELECT * from userhabits where habit_id ='" . $weightID . "'and user_id = '" . $userid . "';"; 

$weightcheck = mysqli_query($conn, $weightCheckquery) or die("2:name check query failed");

if(mysqli_num_rows($weightcheck) >0)
{
	$updateWeight = "UPDATE userhabits SET amount = '" . $weight . "' WHERE user_id = '" . $userid . "' AND habit_id = '" . $weightID . "';"; 
mysqli_query($conn, $updateWeight) or die("4: update to updateWeight failed"); //insert data or error for failure
	echo "updateWeight:" . $updateWeight . "<br>";

  echo "3: habit already exists";
  
}
else
{
	$insertWeight = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $weightID . "','" . $weight . "');";
	mysqli_query($conn, $insertWeight) or die("4: insert insertWeight failed"); //insert data or error for failure
	echo "insertWeight:" . $insertWeight . "<br>";
echo "new insertWeight for this yeser";
}

echo "<br> ---------------------------------<br>End of insert/update for #insertWeight  attempts <br> ---------------------------------<br>";

//




$conn->close();

?>