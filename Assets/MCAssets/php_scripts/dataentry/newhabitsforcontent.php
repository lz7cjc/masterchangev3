<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "lz7cj-2go-u-259938";
*/
//remote machine
$servername = "10.16.16.15";
$username = "lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "lz7cj-2go-u-259938";


//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

//check connection
if ($conn->connect_error) 
{

	die("Connection failed " . $conn->connect_error);

}

//store information entered in form as variables to insert into DB
$contentid = $_GET['contentid'];



$sql_contents= "SELECT
  *
FROM contents where contents.Content_ID = " . $contentid;

$result = mysqli_query($conn, $sql_contents) or die("2:name check query failed");
if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;

}

foreach($dbdata as $item):
      echo "<p3>Title</p3><br><p2>" . $item['ContentTitle'] . "</p2><br><p3>Description</p3><br><p2>" . $item['ContentBody'] . "</p2>" ;

 endforeach;


//get habits  options for drop down list
$sql3= "SELECT
  habits.Habit_ID,
  habits.label,
  habits.description,
  habits.minvalue,
  habits.`maxvalue`,
  habits.boolean,
  habits.habitsectionid
FROM habits";
 
//Prepare the select statement.
$result3 = mysqli_query($conn, $sql3) or die("2:name check query failed");

if ($result3->num_rows > 0) 
{
   $dbdata3 = array();
}
while ($row = $result3->fetch_assoc())
{
$dbdata3[]=$row;

}
//get behavious types already entered for this piece of content
$sql4= "SELECT
  content_habitparameter.Content_ID,
  content_habitparameter.Min_Value,
  content_habitparameter.Max_Value,
  content_habitparameter.yesorno,
  content_habitparameter.Section_Generic,
  contents.ContentTitle,
  contents.ContentBody,
  habits.label,
  habits.description
FROM content_habitparameter
  INNER JOIN contents
    ON content_habitparameter.Content_ID = contents.Content_ID
  INNER JOIN habits
    ON content_habitparameter.Habit_ID = habits.Habit_ID
WHERE content_habitparameter.Content_ID ='" . $contentid . "'";
 echo $sql4;
//Prepare the select statement.
$result4 = mysqli_query($conn, $sql4) or die("2:name check query failed");

if ($result4->num_rows > 0) 
{
   $dbdata4 = array();
}
while ($row = $result4->fetch_assoc())
{
$dbdata4[]=$row;

}
echo "<br><p3>Habits</p3><br>";

foreach($dbdata4 as $item):
      echo 
      "<p2>" . $item['label'] . "</p2>------->>>>>" . 
      "<p2>" . $item['description'] . "</p2>" . 
      "<table><tr><td><p2>Min Value: " . $item['Min_Value'] . "</p2></td>" .
  "<td><p2>Max Value: " . $item['Max_Value'] . "</p2></td>" . 
     "<td><p2>Yes or No: " . $item['yesorno'] . "</p2></td>" . 
     "<td><p2>Everywhere: " . $item['Section_Generic'] . "</p2></td></tr></table>" 
          ;

 endforeach;


?>
<!DOCTYPE html>
<html>
<head>
  <link rel="stylesheet" href="mystyle.css">
  <title></title>
</head>
<body >


<form action="habitsentry.php" method="post">


<select name='habits' size=16>
    <?php foreach($dbdata3 as $item3): ?>
        <option value="<?= $item3['Habit_ID']  ; ?>"><?= $item3['label'] . "---" . $item3['description']; ?></option>
   <?php endforeach; ?>  </select>
<table>
  <tr>
   <td><p2>Min Value:</p2></td>
  <td>  <input type="text" id="minvalue" name="minvalue" style="width: 60%; height: 50px"'> </td>
   <td><p2>Max Value:</p2></td>
  <td>  <input type="text" id="maxvalue" name="maxvalue" style="width: 60%; height: 50px"> </td>
<td><p2>Yes or No</p2></td>
  <td>  <input type="text" id="yesorno" name="yesorno" style="width: 60%; height: 50px"'> </td>
<td><p2>Everywhere</p2></td>
  <td>  <input type="text" id="everywhere" name="everywhere" style="width: 60%; height: 50px"'> </td>

</tr>


 </table>
   <input type="hidden" id="contentid" name="contentid" value="<?=  $item['Content_ID'];?>">

   <input type="hidden" id="ContentTitle" name="ContentTitle" value="<?=   $item['ContentTitle'];?>">

   <input type="hidden" id="ContentBody" name="ContentBody" value="<?=   $item['ContentBody'];?>">
 <input type="hidden" id="label" name="label" value="<?=   $item['label'];?>">

   <input type="hidden" id="description" name="description" value="<?=   $item['description'];?>">

 <input type="submit" name="commit" value="Submit">
</form>
<p>
  <a href="home.html">Home</a>
</body>
</html>
