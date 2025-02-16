<?php
require("dbconnect.php");
 
 $phpname = "secondgoatit";
 $phppassword = "fagfadshgadfh";
 // $userid = "1"


$user_idquery = "SELECT User_id, Username, Fname from users where Username='" . $phpname . "'and password = '". $phppassword . "';"; 
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


echo $outuserid;

//get content 

$sqlspecific = "SELECT DISTINCT
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


$resultspecific = $conn->query($sqlspecific);

if ($resultspecific->num_rows > 0) 
{
   $dbresultspecific = array();
}

while ($row = $resultspecific->fetch_assoc())
{
$dbresultspecific[]=$row;
}
$emptyarray = empty($dbresultspecific);

//echo $emptyarray;
if ($emptyarray != 1)
{
$dataspecific = array("dataspecific" => $dbresultspecific);
echo json_encode($dataspecific);
}



$sqlgeneric = "SELECT
  contents.ContentBody,
  contents.ContentType
FROM contents
WHERE contents.Generic = 1
OR contents.Generic = 2";



$resultgeneric = $conn->query($sqlgeneric);

if ($resultgeneric->num_rows > 0) 
{
   $dbdatageneric = array();
}
else
{
echo "none";
}
while ($row = $resultgeneric->fetch_assoc())
{
$dbdatageneric[]=$row;
}
$emptyarray = empty($dbdatageneric);

//echo $emptyarray;
if ($emptyarray != 1)
{
$datageneric = array("datageneric" => $dbdatageneric);
echo json_encode($datageneric);
}


$conn->close();

?>