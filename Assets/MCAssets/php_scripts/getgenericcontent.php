<?php

require("dbconnect.php");
 

// seperate into different files
//this is a new branch

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