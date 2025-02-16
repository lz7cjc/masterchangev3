<?php
require("dbconnect.php");

//from Unity 
$userid = $_POST["user"];
$film = $_POST["url"];
//hardcoded
/*  $userid = "352";
$film = "https://youtu.be/zRgdT2m51Bc";*/

//////////////////////////////////////////////
///////////Number of tracked days/////////////
//////////////////////////////////////////////
$favourites =  "SELECT FilmID as URL  FROM filmvotes WHERE UserID = '". $userid  . "' and Voted = '1500' and FilmID = '" . $film . "';"; 

//echo "favourites is " . $favourites;
mysqli_query($conn, $favourites) or die("4: no film info"); //insert data or error for failure
$result  = $conn->query($favourites);

if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata=$row;
}
if ($result -> num_rows > 0)
{
echo "1";
}
else 
echo "0";

$conn->close();



?>
