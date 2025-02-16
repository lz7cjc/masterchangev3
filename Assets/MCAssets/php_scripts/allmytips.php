<?php
require("dbconnect.php");

//get user id from unity
$userid = $_POST["userid"];
$contenttype = $_POST["contenttype"];
$title = $_POST["title"];

//hardcode userid for testing
/*$userid = "373";
$contenttype = 2;
$title = 0;*/

//echo "title - :" . $title. "<br>";

if ($title == 1)
  {
      $sql = "Select Distinct

    contents.Content_ID,
    contents.ContentTitle,
    contents.ContentBody
 
From
    userhabits Inner Join
    users On users.User_ID = userhabits.user_id Inner Join
    content_habitparameter On content_habitparameter.Habit_ID = userhabits.habit_id Inner Join
    contents On content_habitparameter.Content_ID = contents.Content_ID

Where
contents.Content_ID IN 
        (       
        Select  contents.Content_ID
        From
            user_behaviour Inner Join
            content_behaviours On content_behaviours.behaviourtype_id = user_behaviour.behaviourtypeid Inner Join
            contents On contents.Content_ID = content_behaviours.content_id
        Where
            user_behaviour.userid = '" . $userid . "' and interested = true
        )
    And
        ((userhabits.amount <= content_habitparameter.Max_Value Or
            userhabits.amount >= content_habitparameter.Min_Value Or
            userhabits.yesorno = content_habitparameter.yesorno) And
                              users.User_ID = '" . $userid . "' )
    And 
           ( contents.ContentType = '" . $contenttype . "')
   union
   Select     contents.Content_ID,
    contents.ContentTitle,
    contents.ContentBody
   from contents 
   where
   ( (contents.Generic = 1 Or
    contents.Generic = 2)
    And
              contents.ContentType = '" . $contenttype . "')
           ORDER BY RAND() LIMIT 1";

  }
else
  {


$sql = "Select Distinct

    contents.Content_ID,
    contents.ContentBody
 
From
    userhabits Inner Join
    users On users.User_ID = userhabits.user_id Inner Join
    content_habitparameter On content_habitparameter.Habit_ID = userhabits.habit_id Inner Join
    contents On content_habitparameter.Content_ID = contents.Content_ID

Where
contents.Content_ID IN 
        (       
        Select  contents.Content_ID
        From
            user_behaviour Inner Join
            content_behaviours On content_behaviours.behaviourtype_id = user_behaviour.behaviourtypeid Inner Join
            contents On contents.Content_ID = content_behaviours.content_id
        Where
            user_behaviour.userid = '" . $userid . "' and interested = true
        )
    And
        ((userhabits.amount <= content_habitparameter.Max_Value Or
            userhabits.amount >= content_habitparameter.Min_Value)
             Or
             (userhabits.yesorno IS NOT NULL
AND content_habitparameter.yesorno = userhabits.yesorno)
           And
                              users.User_ID = '" . $userid . "' )
    And 
           ( contents.ContentType = '" . $contenttype . "')
   union
   Select     contents.Content_ID,
     contents.ContentBody
   from contents 
   where
   ( (contents.Generic = 1 Or
    contents.Generic = 2)
    And
              contents.ContentType = '" . $contenttype . "')
           ORDER BY RAND() LIMIT 1";

  }
//echo $sql;
$result = $conn->query($sql);
//echo "result" . $result;
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