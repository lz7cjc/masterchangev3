<?php
require("dbconnect.php");
//from Unity 
$userid = $_POST["user"];

//hardcoded
//$userid = "373";
//check if record already exists

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////
//////////////Get the questions///////////////
//////////////////////////////////////////////

$sql = "SELECT 
  behaviour_type.Behaviour_Type, 
  quizquestions.question, 
  quizquestions.cost, 
  quizquestions.prize, 
  quizquestions.quiz_id
FROM quizquestions
INNER JOIN behaviour_type ON quizquestions.behaviourtype_id = behaviour_type.behaviourtype_id
LEFT JOIN (
  SELECT qu_id 
  FROM correct_user_questions 
  WHERE userid = '373'
) AS correct_user_questions ON quizquestions.quiz_id = correct_user_questions.qu_id
LEFT JOIN behaviour_questions ON quizquestions.quiz_id = behaviour_questions.quiz_id
LEFT JOIN user_behaviour ON behaviour_questions.behaviourtype_id = user_behaviour.behaviourtypeid AND user_behaviour.userid = '" . $userid . "'
WHERE 
  (user_behaviour.interested = 1 OR user_behaviour.interested IS NULL)
  AND correct_user_questions.qu_id IS NULL
  AND quizquestions.quiz_id NOT IN (
    SELECT qu_id 
    FROM correct_user_questions 
    WHERE userid = '" . $userid . "'
  )
  AND behaviour_questions.behaviourtype_id IN (
    SELECT behaviourtypeid 
    FROM user_behaviour 
    WHERE interested = 1 AND userid = '" . $userid . "');";



  $sql;
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
