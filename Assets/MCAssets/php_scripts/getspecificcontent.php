<?php

require("dbconnect.php");
//$userid = $_POST["username"];

  $userid = "1"



//get contents 

$sqlspecific = "SELECT DISTINCT
   contents.ContentTitle,
   contents.ContentBody,
   user_habits.value,
  user_habits.boolean,
  users.User_ID

FROM user_habits
       INNER JOIN users
         ON user_habits.user_id = users.User_ID,
     content_habitparameter
       INNER JOIN contents
         ON content_habitparameter.Content_ID = contents.Content_ID
WHERE users.User_ID = " . $outuserid . "
AND ((user_habits.value >= content_habitparameter.Min_Value
and user_habits.value <= content_habitparameter.Max_Value)
OR user_habits.boolean = content_habitparameter.Boolean)";


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



$conn->close();

?>