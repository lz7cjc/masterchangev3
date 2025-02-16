<?php
require("dbconnect.php");
//from Unity 
$userid = $_POST["user"];

//hardcoded
/*$userid = "170";*/
//check if record already exists

//////////////////////////////////////////////
//////////////Worst Drink in a day///////////////////////
//////////////////////////////////////////////

$worstdrinkperday =  "Select 
   drinktype.drinkname as drink,
   drinkusage.amount,
   drinktype.units,
    max(Distinct drinkusage.amount * drinktype.units) As totalunits,
    drinkusage.dateofdrink
From
    drinkusage Inner Join
    drinktype On drinktype.drinkid = drinkusage.drinkid
Where
    drinkusage.userid ='" . $userid  . "'
Group By
    drinkusage.userid,
    drinktype.drinkname,
    drinkusage.amount,
    drinktype.units,
    drinkusage.dateofdrink  Order By totalunits desc limit 1;";

//echo "worstDay is " . $worstdrinkperday;
mysqli_query($conn, $worstdrinkperday) or die("4: worstDay"); //insert data or error for failure
$result  = $conn->query($worstdrinkperday);
if ($result->num_rows > 0) 
{
   $dbWorst = array();


while ($row = $result->fetch_assoc())
{
$dbWorst[]=$row;

}
//$dataWorst = array("data" => $dbWorst);

}

echo json_encode($dbWorst);

$conn->close();

?>
