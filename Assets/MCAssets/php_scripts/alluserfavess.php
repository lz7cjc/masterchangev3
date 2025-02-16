<?php
require("dbconnect.php");

//from Unity 
////$film = $_POST["url"];
$userid = $_POST["userid"];
//hardcoded
 // $userid = "361";
/*$film = "https://youtu.be/zRgdT2m51Bc";*/

//////////////////////////////////////////////
///////////Get all their favourites/////////////
//////////////////////////////////////////////
$favourites =  "SELECT FilmID as URL FROM `filmvotes` WHERE UserID='" . $userid . "' and Voted = '1500' "; 
//echo "favourites is " . $favourites;
mysqli_query($conn, $favourites) or die("4: no film info"); //insert data or error for failure
$result  = $conn->query($favourites);
if ($result->num_rows > 0) 
{
   $dbdata = array();
while ($row = $result->fetch_assoc())
{
 // echo $row;
$dbdata[]=$row;
}
$data = array("data" => $dbdata);

echo json_encode($data);

} 
/*else 
{
    echo "0 results";
}*/

$conn->close();

?>
