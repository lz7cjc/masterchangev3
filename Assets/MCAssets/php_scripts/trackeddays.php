<?php
require("dbconnect.php");
//from Unity 
$userid = $_POST["user"];

//hardcoded
// $userid = "170";


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

}

    echo json_encode($dbTracked);


$conn->close();

?>
