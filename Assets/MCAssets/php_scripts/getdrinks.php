<?php


require("dbconnect.php");


//variables from Unity
    $userid = $_POST["dbuserid"];
    $dateofdrink = $_POST["whichday"];
 	
   //hard coded for testing
   // $userid = "2147483";
   // $dateofdrink = "0";

$currentDate = new DateTime();
$offset = "P".$dateofdrink."D";
//echo $offset;
 
//Subtract a day using DateInterval
$consumedDate = $currentDate->sub(new DateInterval($offset));
 
//Get the date in a YYYY-MM-DD format.
$consumedDate = $consumedDate->format('Y-m-d');
//echo "consumed date" . $consumedDate;

 	
$selectdrinks = "Select
    drinkusage.drinkid,
    drinkusage.amount
    
From
drinktype Inner Join
    drinkusage On drinkusage.drinkid = drinktype.drinkid

Where
    drinkusage.userid = '" . $userid . "' and drinkusage.dateofdrink = '" . $consumedDate . "';";


//echo $selectdrinks . "<br><br>";

 mysqli_query($conn, $selectdrinks) or die("4: Get Drinks Failed"); //insert data or error for failure
$result  = $conn->query($selectdrinks);
if ($result->num_rows > 0) 
{
   $dbdata = array();


while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}
$data = array("data" => $dbdata);
echo json_encode($data);
}




$conn->close();

?>