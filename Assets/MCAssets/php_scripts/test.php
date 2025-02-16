<!DOCTYPE html>
<html>
<head>
    <title>dshdsh</title>
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
will this appear?
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


 <input type="submit" name="button" value="Submit">
</form> 

</body>
</html>