<?php

/
//DB connection - change in included file
require("dbconnect.php");



 //echo "success in connecting... next tip types <BR>";

//get user id from unity
$userid = $_POST["user"];
$type = $_POST["type"];
$feedback = $_POST["feedback"];

//hardcode userid for testing
/*$userid = "150";
$type = "Suggestion";
$feedback = "you are a star code maker";
*/
$sql = "INSERT INTO feedback (userid, typeoffeedback, feedback)
  VALUES ('" . $userid . "','" . $type . "','" . $feedback  . "')";
echo $sql;
  mysqli_query($conn, $sql); 
  /*or die("4: failed); //insert data or error for failure
  echo "succeeded;
*/

/*
if ($result->num_rows > 0) 
{
   $dbdata = array();
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}
$data = array("data" => $dbdata);

echo json_encode($data);

} else 
{
    echo "0 results";
}*/



$conn->close();

?>