<?php

require("dbconnect.php");

//from app
 $username = $_POST["c_username"];
 $code = $_POST["c_code"];
 $newpassword = $_POST["c_password"];


//hardcoded for testing
/*  $username = "lz7cjc";
 $code = "kTNq4";
 $newpassword = "masterchange02";*/

 
 //get hashed password from DB based on username
 
$checkuser = "SELECT users.User_ID FROM passwordRecover INNER JOIN users
    ON passwordRecover.userid = users.User_ID WHERE passwordRecover.code = '" . $code . "'AND users.Username = '" . $username . "';"; 
 // echo "Checkuser: " .$checkuser;  
$result = mysqli_query($conn, $checkuser) or die("2:name check query failed");

while ($row = $result->fetch_assoc())
	{
		$dbdata[]=$row;
	}

$data = json_encode($dbdata);
foreach($dbdata as $item)
		{
				$userid = $item['User_ID'];
	//			echo "<br> user id is: " . $userid;
		}

if ($userid !="")
	{
		$hash = password_hash($newpassword, PASSWORD_DEFAULT);

//now update with new password into user table
	//	echo "<br>now we're motoring";
		$updatePassword = "Update users SET Hash ='" . $hash . "' where users.User_ID = '" . $userid . "';";
	//	echo "<br>updatePassword SQL: " . $updatePassword;
		$updatepw = mysqli_query($conn, $updatePassword) or die("2:Update Password failed");
		//echo "<br>updatepw" . $updatepw;

		$deleteCode = "DELETE FROM passwordRecover where userid='" . $userid . "';";
	
		$deleterecord = mysqli_query($conn, $deleteCode) or die("2:Delete record failed");
		echo $userid;
	}
	else
	{
		
	//	echo "bugger";
		echo "0";
	}

$conn->close();

?>