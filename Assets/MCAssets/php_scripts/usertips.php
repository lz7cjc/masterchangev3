<?php
require("dbconnect.php");

 //echo "success in connecting... next tip types <BR>";

//get user id from unity
$userid = $_POST["userid"];

//hardcode userid for testing
//$userid = "130";
/*
$sql1 = "SELECT DISTINCT
  contents.ContentTitle,
  contents.ContentBody
FROM contents
WHERE generic = 1";


$result1 = $conn->query($sql1);
if ($result1->num_rows > 0) 
{
   $dbdata1 = array();
while ($row1 = $result1->fetch_assoc())
{
$dbdata1[]=$row1;
}}
$dbdata1 = json_encode($dbdata1); 
$dbdata1 = trim($dbdata1, "[");
//echo $dbdata1;
//echo json_encode($dbdata1);

*/
$sql = "SELECT DISTINCT
  contents.ContentTitle,
  contents.ContentBody
FROM content_habitparameters
  INNER JOIN contents
    ON contents_habitparameters.Content_ID = contents.Content_ID
  INNER JOIN user_habits
    ON user_habits.habit_id = content_habitparameters.Habit_ID
WHERE user_habits.user_id ='" . $userid . "' AND (user_habits.value >= content_habitparameters.Min_Value
AND user_habits.value <= content_habitparameters.Max_Value
OR user_habits.yesorno = content_habitparameters.yesorno)";

$result = $conn->query($sql);

if ($result->num_rows > 0) 
{
   $dbdata = array();
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}
$data = array("data" => $dbdata);
//$dbdata = json_encode($dbdata); 
//$dbdata = trim($dbdata, "[");
//echo json_encode($result_data);
echo json_encode($data);

/*$result_data = array("data" => json_encode($dbdata) . json_encode($dbdata1));
*///echo $result_data;
//print array in JSON format
 // output data of each row
    /*while($row = $result->fetch_assoc()) {
        echo " - ContentTitle: " . $row["contents.ContentTitle"] . " -   contents.ContentBody: " . $row["contents.ContentBody"]  . "<br><br>";
   }*/
} else 
{
    echo "0 results";
}



$conn->close();

?>