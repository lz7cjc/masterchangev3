<?php

require("dbconnect.php");
//variables from Unity
	$userid = $_POST["dbuserid"];
 	

//variables hardcoded
/*	$userid = "170";*/
 	

$selecthabits = "Select
    habits.Habit_ID,
    habits.label,
    userhabits.amount,
    userhabits.yesorno,
    userhabits.user_id
From
    habits Inner Join
    userhabits On userhabits.habit_id = habits.Habit_ID
Where
    userhabits.user_id = '" . $userid . "';";

//echo $selecthabits;

 mysqli_query($conn, $selecthabits) or die("4: Get Habits Failed"); //insert data or error for failure
$result  = $conn->query($selecthabits);
if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}
$data = array("data" => $dbdata);
echo json_encode($data);




$conn->close();

?>