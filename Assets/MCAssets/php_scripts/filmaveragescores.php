<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "masterchange";
*/

//remote machine
/*$servername = "10.16.16.15";
$username = "lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "lz7cj-2go-u-259938";*/

require("dbconnect.php");



//dynamic

 $phpfilmid= $_POST["filmURL"];

 
 //hard coded
// $phpfilmid= "https://youtu.be/1Mqbo-1VPfw";

//remote
$votesquery = "SELECT AVG(Voted) as score FROM `filmvotes` WHERE FilmID='" . $phpfilmid . "'
";




mysqli_query($conn, $votesquery) or die("4: insert player query failed"); 


$result = $conn->query($votesquery);

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
echo json_encode($dbdata);
}



$conn->close();

?>