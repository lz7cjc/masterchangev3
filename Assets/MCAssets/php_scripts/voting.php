<?php
require("dbconnect.php");

 //echo "success in connecting... next tip types <BR>";

//get user id from unity
$finance = $_POST["finance"];
$Alcohol = $_POST["Alcohol"];
$Smoking = $_POST["Smoking"];
$Youth = $_POST["Youth"];
$Obesity = $_POST["Obesity"];
$Oral = $_POST["Oral"];
$Anxiety = $_POST["Anxiety"];
$Drug = $_POST["Drug"];
$Sexual = $_POST["Sexual"];
$counselling = $_POST["counselling"];
$roleplay = $_POST["roleplay"];
$tracking = $_POST["tracking"];
$badges = $_POST["badges"];
$mentors = $_POST["mentors"];
$graphics = $_POST["graphics"];
$multiplayer = $_POST["multiplayer"];
$graphs = $_POST["graphs"];
$marketplace = $_POST["marketplace"];
$userid = $_POST["user"];

//hardcode userid for testing
/*$finance = "finance";
$Alcohol = "Alcohol";
$Smoking = "Smoking";
$userid = "User";
$Youth = "Youth";
$Obesity = "Obesity";
$Oral = "Oral";
$Anxiety = "Anxiety";
$Drug = "Drug";
$Sexual = "Sexual";
$counselling = "counselling";
$roleplay = "roleplay";
$tracking = "tracking";
$badges = "badges";
$mentors = "mentors";
$graphics = "graphics";
$multiplayer = "multiplayer";
$graphs = "graphs";
$marketplace = "marketplace";
$userid = "150";*/


/* testing some blanks
$finance = "";
$Alcohol = "Alcohol";
$Smoking = "";
$userid = "User";
$Youth = "Youth";
$Obesity = "";
$Oral = "Oral";
$Anxiety = "Anxiety";
$Drug = "Drug";
$Sexual = "Sexual";
$counselling = "";
$roleplay = "roleplay";
$tracking = "tracking";
$badges = "badges";
$mentors = "mentors";
$graphics = "";
$multiplayer = "multiplayer";
$graphs = "graphs";
$marketplace = "marketplace";

$userid = "180";*/

$sql = "INSERT INTO voting (label, userid) VALUES 
('" .$finance . "','" . $userid . "'),
('" . $Alcohol . "','" . $userid . "') ,
('" . $Smoking . "','" . $userid . "'),
('" . $Youth . "','" . $userid . "') ,
('" . $Obesity . "','" . $userid . "'),
('" . $Oral . "','" . $userid . "'),
('" . $Anxiety . "','" . $userid . "'),
('" . $Drug . "','" . $userid . "'),
('" . $Sexual . "','" . $userid . "'),
('" . $counselling . "','" . $userid . "'),
('" . $roleplay . "','" . $userid . "'),
('" . $tracking . "','" . $userid . "'),
('" . $badges . "','" . $userid . "'),
('" . $graphics . "','" . $userid . "'),
('" . $mentors . "','" . $userid . "'),
('" . $multiplayer . "','" . $userid . "'),
('" . $graphs . "','" . $userid . "'),
('" . $marketplace . "','" . $userid .  "')";
echo $sql;
  mysqli_query($conn, $sql); 
$sql1 = "DELETE
  FROM voting
WHERE label = ''";
echo $sql1;
  mysqli_query($conn, $sql1); 
  /*or die("4: failed); //insert data or error for failure
  echo "succeeded;
*/

/*
if ($result->num_rows > 0) 
{
   $dbdata = array();
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}
$data = array("data" => $dbdata);

echo json_encode($data);

} else 
{
    echo "0 results";
}*/



$conn->close();

?>