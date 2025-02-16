<?php

//DB connection - change in included file
require("dbconnect.php");


//from unity

$userid = $_POST["userid"];
$description = $_POST["description"];

//Hard coded values

/*$userid = 342;
$description = "Smoking%";*/

//search for records for user with stated description
	$rirosPaid = "Select Distinct rirosUserid from riros WHERE userid = '" . $userid ."' and description LIKE '" . $description . "';"; 
	//echo $rirosPaid . "<br>";

	
//did it return any records
$result = $conn->query($rirosPaid);
	if ($result->num_rows > 0) 
	{
	 $finalEarnt=1;
	}
	else
	{
	$finalEarnt=0;
	}
		
echo '{"Description":"' . $description . '","Paid": ' . $finalEarnt . '}';
 


$conn->close();

?>