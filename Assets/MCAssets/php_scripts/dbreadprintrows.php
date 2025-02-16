<?php


//DB connection - change in included file
require("dbconnect.php");


 //echo "success in connecting... next tip types <BR>";

$sql = "SELECT
  contents.ContentTitle,
  contents.ContentBody,
  contents.ContentURL,
  content_type.ContentName,
  content_type.ContentDescription,
  behaviour_type.Behaviour_Type,
  behaviour_type.Behaviour_Description
FROM contents
  INNER JOIN content_type
    ON contents.ContentType = content_type.ContentType_ID
  INNER JOIN content_behaviour
    ON content_behaviour.content_id = contents.Content_ID
  INNER JOIN behaviour_type
    ON content_behaviour.behaviourtype_id = behaviour_type.BehaviourType_ID
WHERE behaviour_type.BehaviourType_ID = 1";

$result = $conn->query($sql);

if ($result->num_rows > 0) 
{
   $dbdata = array();
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}

//print array in JSON format
echo json_encode($dbdata);
 // output data of each row
    while($row = $result->fetch_assoc()) {
        echo " - ContentTitle: " . $row["ContentTitle"] . " -   contents.ContentBody: " . $row["ContentBody"]  . " - content_type.ContentName: " . $row["ContentName"] . "content_type.ContentDescription " . $row["ContentDescription"] . "behaviour_type.Behaviour_Type" . $row["Behaviour_Type"] . "behaviour_type.Behaviour_Description " . $row["Behaviour_Description"]  .  "<br><br>";
   }
} else 
{
    echo "0 results";
}



$conn->close();

?>