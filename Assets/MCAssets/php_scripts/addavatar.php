<?php
require("dbconnect.php");
//from Unity 
$userid = $_POST["user"];
$url = $_POST["avatar"];
//hardcoded
/*$userid = '167777';
$url = "vvvvvavrupdate";*/


$neworupdate =  "Select
    * from user_avatars
      Where      userid = '" . $userid  . "';"; 
echo $neworupdate;
//echo "unitsDrunk is " . $unitsDrunk;
mysqli_query($conn, $neworupdate) or die("4: failed to check if got an avatar"); //insert data or error for failure
$result  = $conn->query($neworupdate);
if ($result->num_rows > 0) 
{
 $updateavatar = "UPDATE user_avatars SET url='" . $url . "' WHERE userid = '" . $userid . "';";
  echo $updateavatar;     
  mysqli_query($conn, $updateavatar) or die("4: failed to update avatar"); //insert data or error for failure
//$result  = $conn->query($updateavatar);
         
}

else 
{
  $insertavatar = "INSERT INTO user_avatars(url, userid) VALUES  ('" . $url .  "','" . $userid . "');";
   echo $insertavatar;
   mysqli_query($conn, $insertavatar) or die("4: failed to insert avatar"); //insert data or error for failure
//$result  = $conn->query($insertavatar);

    

}
$conn->close();
?>
