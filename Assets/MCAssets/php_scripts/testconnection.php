<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "lz7cj-2go-u-259938";*/

//remote machine
// require("dbconnect.php");
/*$servername = "localhost";
$username = "vdl54bm_lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "vdl54bm_lz7cj-2g";*/

//DB connection - change in included file
require("dbconnect.php");




echo "<br> update7";
 

 
 $phpname = "masterchange01";
 
 $nameCheckquery = "SELECT * from users where Username='" . $phpname . "';";
 echo $nameCheckquery;
 

 
 $result = $conn->query($nameCheckquery);
echo "how many records?" . $result->num_rows;
if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}

foreach($dbdata as $item)
		{
				$userid = $item['User_ID'];
				$username = $item['Username'];
		}

		echo 'UserID":"' . $userid .  '", "Username":"' . $phpname  . '"}';

$conn->close();
 ?>


 
