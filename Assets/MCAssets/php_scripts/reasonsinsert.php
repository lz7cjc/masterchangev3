<?php
require("dbconnect.php");

//Values from Unity
$numberofcigs = $_POST["cigsPerDay"];
 $smokingYears = $_POST["yearsSmoked"];
 $quitAttempts = $_POST["previousQuits"];
 $prequit = $_POST["prequit"];
 $quitting = $_POST["quitting"];
 $nodate = $_POST["nodate"];
 $ttfcless = $_POST["ttfcless"];
 $nrt =  $_POST["nrt"];
 $userid = $_POST["user"]; 

//hardcoded values
/* $nrt = "1";
 $prequit = "1";
 $quitting = "0";
  $nodate = "0";
  $ttfcless = "1";
	$userid = "1500";
	$numberofcigs = "4345";
 $smokingYears = "123456";
 $quitAttempts = "90876";*/

  ///////////////////////////////
	//DB values for the habits
	$habitidnrt = "22";
 	$habitidprequit = "20";
 	$habitidquitting = "21";
	$habitidnodate = "19";
 	$habitidttfcless = "24";	
	$habitidttfcmore = "25";
	$habitidcigs = "16";
	$habitidyearsSmoked = "18";
	$habitidquits = "17";

/////////// ************** INSERT AND UPDATE NUMBER OF CIGS SMOKED
$cignumCheckquery = "SELECT * from userhabits where habit_id ='" . $habitidcigs . "'and user_id = '". $userid . "';"; 

$cignumcheck = mysqli_query($conn, $cignumCheckquery) or die("2:name check query failed");

if(mysqli_num_rows($cignumcheck) >0)
{
	$updatecignumvalue = "UPDATE userhabits SET amount = '" . $numberofcigs . "', datetime = '" . date("Y-m-d H:i:s") . "' WHERE user_id = '" . $userid . "'AND habit_id = '" . $habitidcigs ."';"; 
mysqli_query($conn, $updatecignumvalue) or die("4: update to cig number failed"); //insert data or error for failure
	echo "update here you go: " . $updatecignumvalue . "<br>";
 // echo "3: habit already exists";
  
}
else
{
	$insertuserquerycignum = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $habitidcigs . "','" . $numberofcigs .  "');";
	//echo $insertuserquerycignum;
	mysqli_query($conn, $insertuserquerycignum) or die("4: insert numberofcigs failed"); //insert data or error for failure
	//echo "new habt for this yeser";
}

/////////// ************** INSERT AND UPDATE NUMBER OF quitAttempts
$quitAttemptsCheckquery = "SELECT * from userhabits where habit_id ='" . $habitidquits . "'and user_id = '". $userid . "';"; 

$quitAttemptscheck = mysqli_query($conn, $quitAttemptsCheckquery) or die("2:name check query failed");

if(mysqli_num_rows($quitAttemptscheck) >0)
{
	$updatequitAttempts = "UPDATE userhabits SET amount = '" . $quitAttempts . "', datetime = '" . date("Y-m-d H:i:s") . "' WHERE user_id = '" . $userid . "'AND habit_id = '" . $habitidquits ."';"; 
mysqli_query($conn, $updatequitAttempts) or die("4: update to insertuserquitAttempts failed"); //insert data or error for failure
	
 // echo "3: habit already exists";
  
}
else
{
	$insertuserquitAttempts = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $habitidquits . "','" . $quitAttempts .  "');";
	mysqli_query($conn, $insertuserquitAttempts) or die("4: insert insertuserquitAttempts failed"); //insert data or error for failure
//	echo "new habt for this yeser";
}
/////////////////////////////////////////////////////////////////////////////////////////////////
/////////// ************** INSERT AND UPDATE NUMBER OF smokingYears
$smokingYearsCheckquery = "SELECT * from userhabits where habit_id ='" . $habitidyearsSmoked . "'and user_id = '". $userid . "';"; 

$smokingYearscheck = mysqli_query($conn, $smokingYearsCheckquery) or die("2:name check query failed");

if(mysqli_num_rows($smokingYearscheck) >0)
{
	$updatesmokingYears = "UPDATE userhabits SET amount = '" . $smokingYears . "', datetime = '" . date("Y-m-d H:i:s") . "' WHERE user_id = '" . $userid . "'AND habit_id = '" . $habitidyearsSmoked ."';"; 
mysqli_query($conn, $updatesmokingYears) or die("4: update to insertuserquitAttempts failed"); //insert data or error for failure
	
//  echo "3: habit already exists";
  
}
else
{
	$insertusersmokingYears = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $habitidyearsSmoked . "','" . $smokingYears .  "');";
	mysqli_query($conn, $insertusersmokingYears) or die("4: insert insertuserquitAttempts failed"); //insert data or error for failure
//	echo "new habt for this yeser";
}
/////////////////////////////////////////////////////////////////////////////////////////////////
	
//Taking NRT

 //echo "amount of nrt before: " . $nrt . "<br>";
/*
	 if ($nrt == "true")
	 {
	 	$nrt = "1";
	 }
	 elseif ($nrt == "false") 
	 { 	
	 	$nrt = "0";
	 }
	 echo "amount of nrt after flip: " . $nrt . "<br>";*/


/////////// ************** INSERT AND UPDATE NRT USAGE
$nrtCheckquery = "SELECT * from userhabits where habit_id ='" . $habitidnrt . "'and user_id = '". $userid . "';"; 
// echo "nrtCheckquery " . $nrtCheckquery . "<br>";

$nrtnumcheck = mysqli_query($conn, $nrtCheckquery) or die("2:name check query failed");

if(mysqli_num_rows($nrtnumcheck) >0)
{
	$updatenrtvalue = "UPDATE userhabits SET yesorno = '" . $nrt . "', datetime = '" . date("Y-m-d H:i:s") . "' WHERE user_id = '" . $userid . "'AND habit_id = '" . $habitidnrt ."';";
	 echo "sql: " . $updatenrtvalue;
mysqli_query($conn, $updatenrtvalue) or die("4: update to cig number failed"); //insert data or error for failure
	//  echo "3: habit already exists" . "<br>";
	//  echo "updatenrtvalue " . $updatenrtvalue . "<br>";


  
}
else
{
	$insertuserquerynrt = "INSERT INTO userhabits (user_id, habit_id, yesorno) VALUES ('" . $userid . "','" . $habitidnrt . "','" . $nrt .  "');";
	mysqli_query($conn, $insertuserquerynrt) or die("4: insert numberofcigs failed"); //insert data or error for failure
echo "new habit for this user" . "<br>";
echo "nrt info" . $insertuserquerynrt . "<br>";
	//echo "insertuserquerynrt " . $insertuserquerynrt . "<br>";

}



/////////// ************** INSERT AND UPDATE NODATE STAGE USAGE
$nodateCheckquery = "SELECT * from userhabits where habit_id ='" . $habitidnodate . "' and user_id = '". $userid . "';"; 

$nodatenumcheck = mysqli_query($conn, $nodateCheckquery) or die("2:name check query failed");
//echo "nodateCheckquery " . $nodateCheckquery . "<br>";

if(mysqli_num_rows($nodatenumcheck) >0)
{
	$updatequitstagenodate = "UPDATE userhabits SET yesorno = '" . $nodate . "', datetime = '" . date("Y-m-d H:i:s") . "' WHERE user_id = '" . $userid . "' AND habit_id = '" . $habitidnodate ."';";
	
mysqli_query($conn, $updatequitstagenodate) or die("4: update to cig number failed"); //insert data or error for failure
	
 // echo "3: habit already exists";
 //  echo "updatequitstagenodate " . $updatequitstagenodate . "<br>";

}
else
{
	$insertuserquerynodate = "INSERT INTO userhabits (user_id, habit_id, yesorno) VALUES ('" . $userid . "','" . $habitidnodate . "','" . $nodate .  "');";
	mysqli_query($conn, $insertuserquerynodate) or die("4: insert numberofcigs failed"); //insert data or error for failure
//	 echo "new habit for this user" . "<br>";
//	 echo "insertuserquerynodate " . $insertuserquerynodate . "<br>";

}


/////////// ************** INSERT AND UPDATE QUITTING STAGE USAGE
$quittingCheckquery = "SELECT * from userhabits where habit_id ='" . $habitidquitting . "' and user_id = '". $userid . "';"; 

$quittingnumcheck = mysqli_query($conn, $quittingCheckquery) or die("2:name check query failed");

if(mysqli_num_rows($quittingnumcheck) >0)
{
	$updatequitstagequitting = "UPDATE userhabits SET yesorno = '" . $quitting . "', datetime = '" . date("Y-m-d H:i:s") . "' WHERE user_id = '" . $userid . "' AND habit_id = '" . $habitidquitting ."';";
	
mysqli_query($conn, $updatequitstagequitting) or die("4: update to cig number failed"); //insert data or error for failure
	
 // echo "3: habit already exists";
 //  echo "updatequitstagequitting " . $updatequitstagequitting . "<br>";

}
else
{
	$insertuserqueryquitting = "INSERT INTO userhabits (user_id, habit_id, yesorno) VALUES ('" . $userid . "','" . $habitidquitting . "','" . $quitting .  "');";
	mysqli_query($conn, $insertuserqueryquitting) or die("4: insert numberofcigs failed"); //insert data or error for failure
//	echo "new habit for this user" . "<br>";
//	echo "insertuserqueryquitting" . $insertuserqueryquitting . "<br>";

}

/////////// ************** INSERT AND UPDATE PRE QUIT STAGE USAGE
$prequitCheckquery = "SELECT * from userhabits where habit_id ='" . $habitidprequit . "' and user_id = '". $userid . "';"; 

$prequitnumcheck = mysqli_query($conn, $prequitCheckquery) or die("2:name check query failed");
//echo "prequitCheckquery" . $prequitCheckquery . "<br>";

if(mysqli_num_rows($prequitnumcheck) >0)
{
	$updatequitstagePreQuit = "UPDATE userhabits SET yesorno = '" . $prequit . "', datetime = '" . date("Y-m-d H:i:s") . "' WHERE user_id = '" . $userid . "' AND habit_id = '" . $habitidprequit ."';";
	// echo "sql: " . $updatequitstagePreQuit;
mysqli_query($conn, $updatequitstagePreQuit) or die("4: update to cig number failed"); //insert data or error for failure
	
 // echo "3: habit already exists";
//  echo "updatequitstagePreQuit " . $updatequitstagePreQuit . "<br>";

}
else
{
	$insertuserqueryprequit = "INSERT INTO userhabits (user_id, habit_id, yesorno) VALUES ('" . $userid . "','" . $habitidprequit . "','" . $prequit .  "');";
	mysqli_query($conn, $insertuserqueryprequit) or die("4: insert numberofcigs failed"); //insert data or error for failure
//echo "new habit for this user" . "<br>";
		// echo "insertuserqueryprequit" . $insertuserqueryprequit . "<br>";

}

 //TTFC	
/////////// ************** INSERT AND UPDATE TTFC USAGE
$ttfclessCheckquery = "SELECT * from userhabits where habit_id ='" . $habitidttfcless . "'and user_id = '". $userid . "';"; 
echo "ttfc less query" . $ttfclessCheckquery . "<br>";
$ttfclessnumcheck = mysqli_query($conn, $ttfclessCheckquery) or die("2:name check query failed");

$rowcheckttfc = mysqli_num_rows($ttfclessnumcheck);
 echo "ttfcless how many rows " . $rowcheckttfc . "<br>";

if(mysqli_num_rows($ttfclessnumcheck) >0)
{
	$updatettfcless = "UPDATE userhabits SET yesorno = '" . $ttfcless . "', datetime = '" . date("Y-m-d H:i:s") . "' WHERE user_id = '" . $userid . "'AND habit_id = '" . $habitidttfcless ."';";
	 

mysqli_query($conn, $updatettfcless) or die("4: update to cig number failed"); //insert data or error for failure
	
  echo "3: habit already exists";
  echo "updatettfcless " . $updatettfcless . "<br>";
}
else
{
	$insertuserqueryttfcless = "INSERT INTO userhabits (user_id, habit_id, yesorno) VALUES ('" . $userid . "','" . $habitidttfcless . "','" . $ttfcless .  "');";
	mysqli_query($conn, $insertuserqueryttfcless) or die("4: insert numberofcigs failed"); //insert data or error for failure
	 echo "new habit for this user" . "<br>";
	 echo "insertuserqueryttfcless " . $insertuserqueryttfcless . "<br>";

}








$conn->close();

?>