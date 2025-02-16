<?php
require("dbconnect.php");
 //echo "success in connecting... next tip types <BR>";

//get user id from unity
//$userid = $_POST["userid"];

//hardcode userid for testing
$userid = "130";

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


}
echo json_encode($dbdata1);










//$result_data = array("data" => $dbdata);

//print array in JSON format
//echo json_encode($result_data);
 // output data of each row
    /*while($row = $result->fetch_assoc()) {
        echo " - ContentTitle: " . $row["contents.ContentTitle"] . " -   contents.ContentBody: " . $row["contents.ContentBody"]  . "<br><br>";
   }*//**/
} else 
{
    echo "0 results";
}



$conn->close();

?>