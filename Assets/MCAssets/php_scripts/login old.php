<?php

require("dbconnect.php");

 
 $phpname = "secondgoatit";
 $phppassword = "fagfadshgadfh";
 // $userid = "1"

/*$userid = "SELECT * from users where Username='" . $phpname . "' AND Password='" . $phppassword . "'";
$getuser = $conn->query($userid);
if ($getuser->num_rows > 0) 
{
   $dbuser = array();
   echo ("dbuser" . $dbuser)
}
else
{
  echo "There is no user with that name";
  die();
}
while ($row = $getuser->fetch_assoc())
{
$hash = $row["Hash"];
//echo $hash . "<br>";
$userid = $row["User_ID"];
//echo $userid . "<br>";
}

$checkhash = password_verify($phppassword, $hash);
if ($checkhash = 1)
{
//echo " password verified<br>";
// echo $phppassword . "<br>";
//echo $hash . "<br>"; 
}
else
{
  echo " password failed <br>";
// echo $password . "<br>";
//echo $hash . "<br>"; 
}
*/
$user_idquery = "SELECT User_id, Username, Fname from users where Username='" . $phpname . "'and password = '". $phppassword . "';"; 
$result = $conn->query($user_idquery);

if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
$outuserid = $row["User_id"];
}
//echo  ("line 78" . json_encode($dbdata));

echo $outuserid;

//get content 

$sqlnew = "SELECT DISTINCT
   contents.ContentTitle,
   contents.ContentBody,
   user_habits.value,
  user_habits.yesorno,
  users.User_ID

FROM user_habits
       INNER JOIN users
         ON user_habits.user_id = users.User_ID,
     content_habitparameters
       INNER JOIN contents
         ON content_habitparameters.Content_ID = contents.Content_ID
WHERE users.User_ID = " . $outuserid . "
AND ((user_habits.value >= content_habitparameters.Min_Value
and user_habits.value <= content_habitparameters.Max_Value)
OR user_habits.yesorno = content_habitparameters.yesorno)";

$result = $conn->query($sqlnew);

if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}
$emptyarray = empty($dbdata);
//echo $emptyarray;
if ($emptyarray != 1)
{
$data = array("data" => $dbdata);
echo json_encode($data);
}


$conn->close();

?>