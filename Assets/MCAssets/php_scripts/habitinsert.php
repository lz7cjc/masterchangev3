<?php

require("dbconnect.php");


//from unity
/*$numberofcigs = $_POST["cigsPerDay"];
 $smokingYears = $_POST["yearsSmoked"];
 $quitAttempts = $_POST["previousQuits"];
$userid = $_POST["user"];*/


//Hard coded values
 $numberofcigs = "7";
  $smokingYears = "1";
  $quitAttempts = "4";
 $userid = "180";

$habitidcigs = "16";
$habitidyearsSmoked = "18";
$habitidquits = "17";


/////////// ************** INSERT AND UPDATE NUMBER OF CIGS SMOKED
$cignumCheckquery = "SELECT * from userhabits where habit_id ='" . $habitidcigs . "'and user_id = '". $userid . "';"; 

$cignumcheck = mysqli_query($conn, $cignumCheckquery) or die("2:name check query failed");

if(mysqli_num_rows($cignumcheck) >0)
{
	$updatecignumvalue = "UPDATE userhabits SET amount = '" . $numberofcigs . "' WHERE user_id = '" . $userid . "'AND habit_id = '" . $habitidcigs ."';"; 

	echo $updatecignumvalue;
mysqli_query($conn, $updatecignumvalue) or die("4: update to cig number failed"); //insert data or error for failure
	
  echo "3: habit already exists";
  
}
else
{
	$insertuserquerycignum = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $habitidcigs . "','" . $numberofcigs .  "');";
	echo $insertuserquerycignum;
	mysqli_query($conn, $insertuserquerycignum) or die("4: insert numberofcigs failed"); //insert data or error for failure
	echo "new habt for this yeser";
}

/////////// ************** INSERT AND UPDATE NUMBER OF quitAttempts
$quitAttemptsCheckquery = "SELECT * from userhabits where habit_id ='" . $habitidquits . "'and user_id = '". $userid . "';"; 

$quitAttemptscheck = mysqli_query($conn, $quitAttemptsCheckquery) or die("2:name check query failed");

if(mysqli_num_rows($quitAttemptscheck) >0)
{
	$updatequitAttempts = "UPDATE userhabits SET amount = '" . $quitAttempts . "' WHERE user_id = '" . $userid . "'AND habit_id = '" . $habitidquits ."';"; 
mysqli_query($conn, $updatequitAttempts) or die("4: update to insertuserquitAttempts failed"); //insert data or error for failure
	
  echo "3: habit already exists";
  
}
else
{
	$insertuserquitAttempts = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $habitidquits . "','" . $quitAttempts .  "');";
	mysqli_query($conn, $insertuserquitAttempts) or die("4: insert insertuserquitAttempts failed"); //insert data or error for failure
	echo "new habt for this yeser";
}
/////////////////////////////////////////////////////////////////////////////////////////////////
/////////// ************** INSERT AND UPDATE NUMBER OF smokingYears
$smokingYearsCheckquery = "SELECT * from userhabits where habit_id ='" . $habitidyearsSmoked . "'and user_id = '". $userid . "';"; 

$smokingYearscheck = mysqli_query($conn, $smokingYearsCheckquery) or die("2:name check query failed");

if(mysqli_num_rows($smokingYearscheck) >0)
{
	$updatesmokingYears = "UPDATE userhabits SET amount = '" . $smokingYears . "' WHERE user_id = '" . $userid . "'AND habit_id = '" . $habitidyearsSmoked ."';"; 
mysqli_query($conn, $updatesmokingYears) or die("4: update to insertuserquitAttempts failed"); //insert data or error for failure
	
  echo "3: habit already exists";
  
}
else
{
	$insertusersmokingYears = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $habitidyearsSmoked . "','" . $smokingYears .  "');";
	mysqli_query($conn, $insertusersmokingYears) or die("4: insert insertuserquitAttempts failed"); //insert data or error for failure
	echo "new habt for this yeser";
}
/////////////////////////////////////////////////////////////////////////////////////////////////

//echo "old date " . $phpdob . " and the new date: " . $dateForSQL. "ends <br><br>";
// if ($checkifhabitexistsSQL = 0)
// 	{
// 		
// 	}
	
// $insertuserquery1 = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $habitidyearsSmoked . "','" . $smokingYears .  "');";
// $insertuserquery2 = "INSERT INTO userhabits (user_id, habit_id, amount) VALUES ('" . $userid . "','" . $habitidquits . "','" . $quitAttempts .  "');";

// mysqli_query($conn, $insertuserquery) or die("4: insert numberofcigs failed"); //insert data or error for failure
// mysqli_query($conn, $insertuserquery1) or die("4: insert yearsSmoked failed"); //insert data or error for failure
// mysqli_query($conn, $insertuserquery2) or die("4: insert quitAttempts failed"); //insert data or error for failure

//show records including new values
// $sqlnew = "SELECT * from users";

// $result = $conn->query($sqlnew);

// if ($result->num_rows > 0) 
// {
//    $dbdata = array();
// while ($row = $result->fetch_assoc())
// {
// $dbdata[]=$row;
// }
//print array in JSON format
// echo json_encode($dbdata);
//  // output data of each row
// $sql = "SELECT * from users";
// $result = $conn->query($sql);
// while($row = $result->fetch_assoc()) 
//    {
//         echo " - fname: " . $row["Fname"] . " -   lname: " . $row["Lname"]  . " - username: " . $row["Username"] . " - hash: " . $row["Hash"] .  " - salt: " . $row["Salt"] . "dob " . $row["DoB"] . "Email" . $row["Email"] . "<br><br>";
//    }
// // }
// } else 
//   {
//     echo "0 results";
//   }



$conn->close();

?>