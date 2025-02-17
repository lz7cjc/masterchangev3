<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "masterchange";*/

//remote machine
$servername = "10.16.16.15";
$username = "lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "lz7cj-2go-u-259938";


//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

if ($conn->connect_error) 
{

	die("Connection failed " . $conn->connect_error);

}



//get dropdowns
//Our select statement. This will retrieve the data that we want.

//get content type
$sql = "SELECT ContentType_ID, ContentName FROM content_type";
 echo "SELECT ContentType_ID, ContentName FROM content_type" . $sql;
//Prepare the select statement.
$result = $conn->query($sql);
//echo $result;
if ($result->num_rows > 0) 
{
   $dbdata = array();
}

while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;

}
//var_dump($dbdata);


//get generic options
$sql1 = "SELECT * FROM generic_options";
  echo "SELECT * FROM generic_options" . $sql1;

//Prepare the select statement.
//$result1 = mysqli_query($conn, $sql1) or die("2:name check query failed");

$result1 = $conn->query($sql1);

if ($result1->num_rows > 0) 
{
   $dbdata1 = array();
}
while ($row1 = $result1->fetch_assoc())
{
$dbdata1[]=$row1;

}

//get habits
$sql2= "SELECT Habit_ID, label, description FROM habits";
 echo "SELECT Habit_ID, label, description FROM habits" . $sql2;

//Prepare the select statement.
$result2 = mysqli_query($conn, $sql2) or die("2:name check query failed");

if ($result2->num_rows > 0) 
{
   $dbdata2 = array();
}
while ($row2 = $result2->fetch_assoc())
{
$dbdata2[]=$row2;

}

//get behavious types options
$sql3= "SELECT BehaviourType_ID	, Behaviour_Type, Behaviour_Description FROM behaviour_type";
  echo "SELECT BehaviourType_ID , Behaviour_Type, Behaviour_Description FROM behaviour_type" . $sql3;

//Prepare the select statement.
$result3 = mysqli_query($conn, $sql3) or die("2:name check query failed");

if ($result3->num_rows > 0) 
{
   $dbdata3 = array();
}
while ($row3 = $result3->fetch_assoc())
{
$dbdata3[]=$row3;

}
//var_dump($dbdata1);

$conn->close();

?>
<!DOCTYPE html>
<head>
  <style type="text/css">
body {
font-size: 12px;
}
h1 {
font-size: 1em;
line-height: 1.5;
font-weight: bold;
margin: 1em 0 0 0;
}
p {
margin: 0;
width: 33em;
}
p.one {
font-size: 18px;
line-height: 1.5em;
font-size: 1em;

font-weight: bold;

}
p.two {
font-size: 1.2em;
line-height: 1.5em;
}
</style>
</head>
<body>

<form action="contententry.php" method="post">
<p class="one">Title </p>
  <br>
    <input type="text" id="title" name="title" style="width: 60%; height: 50px"; 'font-size: 32px;' >
    <br>
    Description
    <br>
<textarea name="Description" rows="10" cols="50"></textarea>
<br>
<select name="typecontent">
    <?php foreach($dbdata as $item): ?>
        <option value="<?= $item['ContentType_ID']; ?>"><?= $item['ContentName']; ?></option>
    <?php endforeach; ?>

        Habits<br> </select>
<select name="where">
    <?php foreach($dbdata1 as $item1): ?>
        <option value="<?= $item1['generic_optionsid']; ?>"><?= $item1['label']; ?></option>
    <?php endforeach; ?>

</select>
<br>
<<!-- select name='habits[]' multiple="multiple" size=60>
    <?php foreach($dbdata2 as $item2): ?>
        <option value="<?= $item2['Habit_ID']  ; ?>"><?= $item2['label'] . "---" . $item2['description']; ?></option>
    <?php endforeach; ?>
</select>
  <br>
  Behaviours
  <br>
<select name='behaviours[]' multiple="multiple" size=16>
    <?php foreach($dbdata3 as $item3): ?>
        <option value="<?= $item3['BehaviourType_ID']  ; ?>"><?= $item3['Behaviour_Type'] . "---" . $item3['Behaviour_Description']; ?></option>
   <?php endforeach; ?>  </select>
 <input type="text" name = "minvalue" value="minvalue">  <input type="text" name = "maxvalue" value="maxvalue">   <input type="text" name = "yesorno" value="boolean1 or 0"> 
 -->

 <input type="submit" name="button" value="Submit">

<script type="text/javascript">
$('Description').keyup(function() {    
    var characterCount = $(this).val().length,
        current_count = $('#current_count'),
        maximum_count = $('#maximum_count'),
        count = $('#count');    
        current_count.text(characterCount);        
});
</script>
</form> 


</body>
</html>

