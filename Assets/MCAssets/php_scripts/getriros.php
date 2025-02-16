<?php

require("dbconnect.php");
//from unity

 $userid = $_POST["userid"];


//Hard coded values

 //$userid = "395";




/////////// ************** Get last Riro entry

	$getallRiros = "Select rirosBought, rirosSpent, rirosEarnt from riros WHERE userid = '" . $userid ."' ORDER BY TIMESTAMP DESC;"; 

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