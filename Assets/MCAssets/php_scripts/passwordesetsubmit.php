<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "masterchange";*/

//remote machine
$servername = "localhost";
$username = "vdl54bm_lz7cj-2go-u-259938";
$password = "eBJ7bEI%zA)#Ldsc";
$dbname = "vdl54bm_lz7cj-2g";


//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

//check connection
if ($conn->connect_error) 
{

	die("Connection failed " . $conn->connect_error);

}

//from app
/* $username = $_POST["c_username"];
 $code = $_POST["c_code"];
 $newpassword = $_POST["c_password"];
*/

//hardcoded for testing
  $username = "lz7cjc";
 $code = "kTNq4";
 $newpassword = "masterchange02";

 
 //get hashed password from DB based on username
 
$checkuser = "SELECT users.User_ID, FROM passwordrecover INNER JOIN users
    ON passwordrecover.userid = users.User_ID WHERE passwordrecover.code = '" . $code . "'AND users.Username = '" . $username . "';"; 
  echo "Checkuser: " .$checkuser;  
$result = mysqli_query($conn, $checkuser) or die("2:name check query failed");

while ($row = $result->fetch_assoc())
	{
		$dbdata[]=$row;
	}

$data = json_encode($dbdata);
foreach($dbdata as $item)
		{
				$userid = $item['User_ID'];
		}

if ($userid !="")
	{
		$hash = password_hash($newpassword, PASSWORD_DEFAULT);

//now update with new password into user table
		echo "now we're motoring";
$updatePassword = "Update users SET Password ='" . $hash . "where user users.User_ID = '" . $userid . "';";
echo "updatePassword SQL: " . $updatePassword;
$updatepw = mysqli_query($conn, $updatePassword) or die("2:Update Password failed");
echo "updatepw" . $updatepw;

$deleteCode = "Delete * from passwordrecover where userid='" . $userid . "' and code ='" . $code . "';";
echo "deleteCode" . $deleteCode;

$deleterecord = mysqli_query($conn, $deleteCode) or die("2:Delete record failed");
echo "1";
	else
	{
		echo "bugger";
		echo "0";
	}

$conn->close();

?>