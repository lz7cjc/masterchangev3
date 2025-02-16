<?php
require("dbconnect.php");
//from Unity 
$userid = $_POST["user"];

//hardcoded
// $userid = "170";
//check if record already exists

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////
//////////////Total Units Drunk///////////////
//////////////////////////////////////////////

$unitsDrunk =  "Select
    Sum(amount * units) As totalunits 
    from drinkusage Inner Join drinktype On drinkusage.drinkid = drinktype.drinkid
      Where      userid = '" . $userid  . "';"; 

//echo "unitsDrunk is " . $unitsDrunk;
mysqli_query($conn, $unitsDrunk) or die("4: unitsDrunk"); //insert data or error for failure
$result  = $conn->query($unitsDrunk);
if ($result->num_rows > 0) 
{
 $dbtotalunits = array();


while ($row = $result->fetch_assoc())
{
$dbtotalunits[]=$row;
}
//$dataTotal = array("data" => $dbtotalunits);

}
 echo json_encode($dbtotalunits);

$conn->close();

?>
