<?php
require("dbconnect.php");
//from Unity 
$questionId = $_POST["questionId"];

//from url
//$questionId = $_GET["questionId"];

//hardcoded
//$questionId = "30";

//check if record already exists

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////
//////////////Get the questions///////////////
//////////////////////////////////////////////

$sql =  "SELECT
  quizanswers.answer,
  quizanswers.correct,
  quizanswers.explanation
FROM quizanswers where quiz_id = '" . $questionId . "';";
//; 
//echo $sql;

mysqli_query($conn, $sql) or die("4: questions"); //insert data or error for failure
$result  = $conn->query($sql);
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
}

 
$conn->close();

?>
