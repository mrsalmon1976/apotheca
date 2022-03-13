<template>
  <div>
    <button @click="callApi">Call</button>
    <p>Stores: {{ storeCount }}</p>
  </div>
</template>

<script>
import axios from "axios";

export default {
  name: "DashboardView",
  data() {
    return {
      storeCount: -1
    };
  },
  methods: {
    async callApi() {
      // Get the access token from the auth wrapper
      const token = await this.$auth.getTokenSilently();
      //alert(token);

      // Use Axios to make a call to the API
      const { data } = await axios.get("https://localhost:6060/api/store/userstores", {
         headers: {
           Authorization: `Bearer ${token}`    // send the access token through the 'Authorization' header
         }
      });

      this.storeCount = data.length;
    }
  }
};
</script>
