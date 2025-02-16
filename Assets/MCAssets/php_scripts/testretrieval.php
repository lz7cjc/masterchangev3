<?php
require("dbconnect.php");
   
 $phpname = "nicktest";
 $passwordfromapp = "nicktest";

   if(! $conn ) {
      die('Could not connect: ' . mysql_error());
   }
   
   $sql = "SELECT Hash from users where Username='" . $phpname . "';"; 
   mysql_select_db('lz7cj-2go-u-259938');
   $retval = mysql_query( $sql, $conn );
   echo $sql;
   if(! $retval ) {
      die('Could not get data: ' . mysql_error());
   }
   
   while($row = mysql_fetch_array($retval, MYSQL_ASSOC)) {
      echo $row['Hash'] .  "<br> ".
        
         "--------------------------------<br>";
   }
   
   echo "Fetched data successfully\n";
$conn->close();


?>