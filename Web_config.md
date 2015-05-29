Bellow is the sample of the custom configuration section for the FeedbackPage library. You don't need any configuration to get started, however, after initial set-up you will want to use at least `writers` subsection in order to specify what happens with the issue after it is submited.

```
<?xml version="1.0"?>
<configuration>
   <configSections>
     <section  name="Feedback"  type="Feedback.Config, Feedback, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0b70fe138b18831f"/>  
   </configSections>
  
   <Feedback  enabled="true" dock="MB" width="400px">

     <header>Some header</header>
     <title><![CDATA[  <br/><em>Some Title</em> ]]> </title>

     <writers>
       <writer enabled ="false" type="Feedback.DefaultWriters.EmailWriter">
         <Smtp>12.35.1.31</Smtp>
         <Email>majki@somehost.com</Email>
       </writer>

       <writer enabled ="true" type="Feedback.DefaultWriters.TextWriter" />
     </writers>

     <exclude>
        http://mydomain.com/DisabledPage.aspx
        http://mydomain.com/DisabledBranch*
     </exclude>

   </Feedback>
```