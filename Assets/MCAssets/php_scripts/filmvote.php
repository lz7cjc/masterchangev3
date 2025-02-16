<?php

$live = $_POST["live"];
//local machine


//remote machine
/*$servername = "10.16.16.15";
$username = "lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "lz7cj-2go-u-259938";*/
 
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "masterchange";
 */

require("dbconnect.php");

//dynamic
$phpvote = $_POST["voteis"];
 $phpfilmid= $_POST["filmid"];
$phpuserid = $_POST["userid"];
//$riros = $_POST["newRiros"];

//static
/* $phpvote = "1000";
 $phpfilmid= "https://youtu.be/MbzJAdS-mjs";
$phpuserid = "0";
$riros = "453";	*/

$insertvote = "INSERT INTO filmvotes (Voted, FilmID, UserID) VALUES ('" . $phpvote . "','" . $phpfilmid . "','" . $phpuserid . "')";

mysqli_query($conn, $insertvote) or die("4: insert player query failed");



$conn->close();

?>