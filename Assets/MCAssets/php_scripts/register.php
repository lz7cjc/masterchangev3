<?php

$require("dbconnect.php");

$username = $_POST["username"];
$password = $_POST["password"];


//check if name exists
$nameCheckquery = "SELECT username from players where username='" . $username	. "';";

$namecheck = mysqli_query($con, $namecheckquery) or die("2:name check query failed");

if(mysqli_num_rows($namecheck) >0)
{
	echo "3: name already exists";
	exit();
}
	
//add user to table
$salt = "\$5\$rounds=500\$" . "steamedhams" . $username	 . "\$";
$hash = crypt($password, $salt);

$insertuserquery = "INSERT INTO `players` (`username`, `hash`, `salt`) VALUES ('" . $username . "', '" . $hash . "', '" . $salt . "');";

mysqli_query($con, $insertuserquery) or die("4: insert player query failed"); //insert data or error for failure

echo ("0");

$conn->close();

?>