<?php
require("dbconnect.php");

//dynamic

 $phpfilmid= $_POST["filmURL"];

 
 //hard coded
/* $phpfilmid= "https://youtu.be/1Mqbo-1VPfw";
*/
//remote
$votesquery = "SELECT Voted, COUNT(1) as count 
FROM filmvotes 
where FilmId = '" . $phpfilmid . "'
GROUP BY Voted 
ORDER BY count DESC;";




mysqli_query($conn, $votesquery) or die("4: insert player query failed"); 


$result = $conn->query($votesquery);

if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}
if ($result -> num_rows > 0)
{
echo json_encode($dbdata);
}



$conn->close();

?>