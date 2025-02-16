<?php


//DB connection - change in included file
require("dbconnect.php");



//dynamic values from form
$phpage = $_POST["age"];
 $phpweight = $_POST["weight"];
 $phpheight = $_POST["height"];
$userid = $_POST["user"];

//static values
/*$phpage = 48;
 $phpweight = 85;
 $phpheight = 180;
$userid = 169;*/

//DB values for the habits
	$habitidage = "26";
 	$habitidweight = "27";
 	$habitidheight = "28";
	

/* echo "age" + $age;
  echo "phpweight" + $weight;
   echo "phpheight" + $height;*/

//check age
    $age_query = "SELECT * from userhabits where habit_id ='" . $habitidage . "' and user_id = '". $userid . "';"; 
	$checkifageexists = mysqli_query($conn, $age_query) ;
	//echo $checkifageexists;
// taken out as don't want to stop; or die("2:name check query failed")
//check weight
    $weight_query = "SELECT * from userhabits where habit_id ='" . $habitidweight . "' and user_id = '". $userid . "';"; 
	$checkifweightexists = mysqli_query($conn, $weight_query);
//check height
   $height_query = "SELECT * from userhabits where habit_id ='" . $habitidheight . "' and user_id = '". $userid . "';"; 
	$checkifheightexists = mysqli_query($conn, $height_query);
/*//check bmi
   $bmi_query = "SELECT * from userhabits where age ='" . $phpbmi . "' and user_id = '". $userid . "';"; 
	$checkifbmiexists = mysqli_query($conn, $habit_id_query) or die("2:name check query failed");
*/


	//insert or update age
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
if(mysqli_num_rows($checkifageexists) >0)
{
  $updateage = "UPDATE userhabits SET value = " . $phpage . " WHERE user_id = '" . $userid . "'AND habit_id = '" . $habitidage ."';";
	 echo "sql: " . $updateage;
mysqli_query($conn, $updateage) or die("4: update to insertage failed" . $phpage); //insert data or error for failure
	
  echo "3: habit already exists";
}
else
{
	$insertage = "INSERT INTO userhabits (user_id, habit_id, value) VALUES ('" . $userid . "','" . $habitidage . "','" . $phpage .  "');";
	mysqli_query($conn, $insertage) or die("4: insert insertage failed" . $insertage); //insert data or error for failure
	echo "new habt for this yeser";
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//insert or update height
if(mysqli_num_rows($checkifheightexists) >0)
{
  $updateheight = "UPDATE userhabits SET value = " . $phpheight . " WHERE user_id = '" . $userid . "'AND habit_id = '" . $habitidheight ."';";
	 echo "sql: " . $updateheight;
mysqli_query($conn, $updateheight) or die("4: update to updateheight failed"); //insert data or error for failure
	
  echo "3: habit already exists";
}
else
{
	$insertheight = "INSERT INTO userhabits (user_id, habit_id, value) VALUES ('" . $userid . "','" . $habitidheight . "','" . $phpheight .  "');";
	mysqli_query($conn, $insertheight) or die("4: insert numberofcigs failed"); //insert data or error for failure
	echo "new habt for this yeser";
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//insert or update WEIGHT $habitidweight $phpweight
if(mysqli_num_rows($checkifheightexists) >0)
{
  $updateweight = "UPDATE userhabits SET value = " . $phpweight . " WHERE user_id = '" . $userid . "'AND habit_id = '" . $habitidweight ."';";
	 echo "sql: " . $updateweight;
mysqli_query($conn, $updateweight) or die("4: update to updateweight failed"); //insert data or error for failure
	
  echo "3: habit already exists";
}
else
{
	$insertweight = "INSERT INTO userhabits (user_id, habit_id, value) VALUES ('" . $userid . "','" . $habitidweight . "','" . $phpweight .  "');";
	mysqli_query($conn, $insertweight) or die("4: insert numberofcigs failed"); //insert data or error for failure
	echo "new habt for this yeser";
}
//and repeat for each attribute


echo "ppppp" . $phpage . $phpweight . $phpheight . $userid;
$conn->close();

?>