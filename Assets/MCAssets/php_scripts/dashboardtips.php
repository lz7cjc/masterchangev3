<?php
require("dbconnect.php");

 //echo "success in connecting... next tip types <BR>";

//get user id from unity
//$userid = $_POST["userid"];

//hardcode userid for testing
//$userid = "130";

$sql = "SELECT DISTINCT
  contents.ContentTitle,
  contents.ContentBody
FROM contents
WHERE generic = 1";


$result = $conn->query($sql);

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
}



$conn->close();

?>