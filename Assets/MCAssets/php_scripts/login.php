<?php
require("dbconnect.php");

////////////////////////////////////////////get values/////////////////////////////
//from app
 $phpname = $_POST["c_username"];

 $passwordfromapp = $_POST["c_password"];


//get username and password
/* $phpname = "localuser";
 $passwordfromapp = "localuserdd";*/

 /////////////////////////////////////////////////////////////////////////////////

 //get hashed password from DB based on username
 
$contenthabits = "SELECT Hash, User_ID from users where Username='" . $phpname . "'"; 
    
$result = mysqli_query($conn, $contenthabits) or die("2:name check query failed");

////////////check if the username exists////////////////////////////////////////////
if ($result->num_rows > 0)
	{
		while ($row = $result->fetch_assoc())
		{
		$dbdata[]=$row;

		}
		$data = json_encode($dbdata);
		//echo $data;
		foreach($dbdata as $item)
			{
			//	echo "in loop" . $item['Hash'];
			$dbhashvalue = $item['Hash']; 
			$dbuserid = $item['User_ID']; 
			}
	}
	else
	{
		die("Your username or password is incorrect. Please try again");
	}

 ////////////////////////////authentication of username and password //////////////////////////////////
  $checkpassword = password_verify($passwordfromapp, $dbhashvalue) ;
 	if (password_verify($passwordfromapp, $dbhashvalue)) 
		{

			$userParameters = "SELECT dbuserid, IntroScreen, SwitchtoVR, SkipLearningScreenInt, creditsgiven, returnToScene, stage, CTstartpoint, delaynotification, behaviour, habitsdone from user_preferences where dbuserid =" . $dbuserid . "; ";

///////////////////////////////get user preferences if the account is verified//////////////////////////////

		 mysqli_query($conn, $userParameters) or die("4: Get Habits Failed"); //insert data or error for failure
		$result  = $conn->query($userParameters);
		if ($result->num_rows > 0) 
		{
		   $dbdata = array();
		}
		while ($row = $result->fetch_assoc())
		{
		$dbdata[]=$row;
		}
		$data = array("data" => $dbdata);
		echo json_encode($data);

		} 
		//////////////////////////failed to authentic user with user name and password/////////////////
		else 
			{
			 die("Your username or password is incorrect. Please try again");
			}


/*
$user_idquery = "SELECT User_id, Username, Fname from users where Username='" . $phpname . "'and password = '". $hash . "';"; 

$useridresult = $conn->query($user_idquery);

if ($useridresult->num_rows > 0) 
{
   $dbuserid = array();
}
while ($row = $useridresult->fetch_assoc())
{
$dbuserid[]=$row;
$outuserid = $row["User_id"];
}
$emptyarray = empty($dbuserid);
//echo $emptyarray;
if ($emptyarray != 1)
{
$userinfo = array("userinfo" => $dbuserid);
echo json_encode($userinfo);
}


*/

$conn->close();

?>