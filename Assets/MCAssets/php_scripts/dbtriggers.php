<?php


//DB connection - change in included file
require("dbconnect.php");


//from unity

$userid = $_POST["userid"];


//Hard coded values

//$userid = "7";



/////////// ************** Get last Riro entry

	$getRiros = "Select rirosBought, rirosSpent, rirosEarnt from riros WHERE userid = '" . $userid ."' ORDER BY TIMESTAMP DESC LIMIT 1;"; 

	//echo $getRiros;
mysqli_query($conn, $getRiros) or die("4: update to riros failed"); //insert data or error for failure
	$result = $conn->query($getRiros);

 if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}
echo json_encode($dbdata);




$conn->close();

?>