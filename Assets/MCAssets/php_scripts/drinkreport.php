<?php

//DB connection - change in included file
require("dbconnect.php");


//from Unity 
//$userid = $_POST["user"];

//hardcoded
$userid = "170";
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


  

//////////////////////////////////////////////
///////////Number of tracked days/////////////
//////////////////////////////////////////////
$trackeddays =  "Select
    Count(distinct dateofdrink) as trackeddays
    from drinkusage
      Where      userid = '" . $userid  . "';"; 

//echo "trackeddays is " . $trackeddays;
mysqli_query($conn, $trackeddays) or die("4: trackeddays"); //insert data or error for failure
$result  = $conn->query($trackeddays);
if ($result->num_rows > 0) 
{
   $dbTracked = array();


while ($row = $result->fetch_assoc())
{
$dbTracked[]=$row;
}
//$datatracked = array("data" => $dbTracked);

}
  echo json_encode($dbtotalunits);
 echo json_encode($dbWorst);
    echo json_encode($dbTracked);


    //  echo json_encode($data);
//average units a day
// number of drink n * units for drink n / total number of distinct days

    
$conn->close();

?>
