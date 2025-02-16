<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "vdl54bm_lz7cj-2g";*/

//remote machine
$servername = "localhost";
$username = "vdl54bm_lz7cj-2go-u-259938";
$password = "eBJ7bEI%zA)#Ldsc";
$dbname = "vdl54bm_lz7cj-2g";


//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

//check connection
if ($conn->connect_error) 
{

	die("Connection failed " . $conn->connect_error);

}

//store information entered in form as variables to insert into DB
$contentid = $_POST['contentid'];
$habitid = $_POST['habits'];
$minvalue = $_POST['minvalue'];
//echo "min valie is what: " . $minvalue;
if (empty($minvalue))
{ 
$minvalue = 'null'; 
echo "min valie is what: " . $minvalue;

}
else
{
  $minvalue = "'" . $minvalue . "'";
 // echo "min valie is what:11" . $minvalue;

}
$maxvalue = $_POST['maxvalue'];
if (empty($maxvalue))
{ 
$maxvalue = 'null'; 
//echo "maxvalue valie is what: " . $maxvalue;

}
else
{
  $maxvalue = "'" . $maxvalue . "'";
 // echo "maxvalue valie is what:11" . $maxvalue;

}

$boolvalue = $_POST['yesorno'];

if (empty($boolvalue))
{ 
$boolvalue = 'null'; 
//echo "boolvalue valie is what: " . $boolvalue;

}
else
{
  $boolvalue = "'" . $boolvalue . "'";
  //echo "boolvalue valie is what:11" . $boolvalue;

}

$label = $_POST['label'];
$description = $_POST['description'];
$generic = $_POST['everywhere'];
if (empty($generic))
{ 
$generic = 'null'; 
//echo "generic valie is what: " . $generic;

}
else
{
  $generic = "'" . $generic . "'";
  //echo "generic valie is what:11" . $generic;

}
$ContentTitle = $_POST['ContentTitle'];
$ContentBody = $_POST['ContentBody'];/////////////////////////////////////////////
//Insert the values posted to this page 
/////////////////////////////////////////////
//////////insert behaviours
if (is_null($habitid))
{
	//echo "no habits to insert" . $habitid;
}
else
{
$inserthabitparameters = "INSERT INTO content_habitparameter (Content_ID, Habit_ID, Min_Value, Max_Value, yesorno, Section_Generic) VALUES ('" . $contentid . "','" .  $habitid . "'," . $minvalue . "," . $maxvalue . "," . $boolvalue . "," . $generic . ")";

//echo $inserthabitparameters;
mysqli_query($conn, $inserthabitparameters) or die("4: failed to insert habits"); //insert data or error for failure

//echo $inserthabitparameters . "<br>";
}
header("Location: contentviewhabits.php");
exit();
?>

