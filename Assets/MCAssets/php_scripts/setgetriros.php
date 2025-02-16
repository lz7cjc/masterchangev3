<?php
require("dbconnect.php");

//from unity
$rirosValue = $_POST["rirosValue"];
$description = $_POST["description"];
 $riroType = $_POST["riroType"];
$userid = $_POST["userid"];


//Hard coded values
//  $rirosValue= "1650";
//  $riroType= "Earnt";
//  $userid = "2587";
// $description = "tip for fim";

if ($riroType == "Bought")
{
$updateRiros = "INSERT INTO riros(userid, rirosBought, description) VALUES ('" . $userid . "', '" . $rirosValue . "', '" . $description . "');";

}

else if ($riroType == "Earnt")
{
$updateRiros = "INSERT INTO riros(userid, rirosEarnt, description) VALUES ('" . $userid . "', '" . $rirosValue . "', '" . $description . "');";

}

else if ($riroType == "Spent")
{
	$updateRiros = "INSERT INTO riros(userid, rirosSpent, description) VALUES ('" . $userid . "', '" . $rirosValue . "', '" . $description . "');";

}
/*echo $updateRiros;*/
mysqli_query($conn, $updateRiros) or die("4: update to riros failed"); //insert data or error for failure
	
 

	$getSumEarntSQL = "Select Sum(rirosEarnt) as Earnt From riros where userid = '" . $userid . "';";
$getSumBoughtSQL = "Select Sum(rirosBought) as Bought From riros where userid = '" . $userid . "';";
$getSumSpentSQL = "Select Sum(rirosSpent) as Spent From riros where userid = '" . $userid . "';";


$result = $conn->query($getSumEarntSQL);
	if ($result->num_rows > 0) 
	{
	  while ($row = $result->fetch_assoc())
		{
		
		$finalEarnt = $row["Earnt"];
			if (is_null($finalEarnt))
			{
				$finalEarnt=0;
			}
		}
 	}

$result1 = $conn->query($getSumBoughtSQL);
	if ($result1->num_rows > 0) 
	{
	  while ($row = $result1->fetch_assoc())
		{
		$finalBought = $row["Bought"];
		if (is_null($finalBought))
			{
				$finalBought=0;
			}
		}
	 }

 $result2 = $conn->query($getSumSpentSQL);
	if ($result2->num_rows > 0) 
	{
		  while ($row = $result2->fetch_assoc())
		{
		$finalSpent = $row["Spent"];

		if (is_null($finalSpent))
			{
				$finalSpent=0;
			}
		}
	 }

$balance = ($finalBought + $finalEarnt) - $finalSpent;
echo '{"Bought": ' . $finalBought .  ', "Earnt":' .$finalEarnt . ', "Spent":' . $finalSpent . '}';
 


$conn->close();

?>